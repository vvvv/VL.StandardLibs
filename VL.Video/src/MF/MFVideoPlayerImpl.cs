#nullable enable
using CommunityToolkit.HighPerformance;
using Stride.Core.Mathematics;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using VL.Video.MF;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D10;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.Graphics.Imaging;
using Windows.Win32.Media.Audio;
using Windows.Win32.Media.MediaFoundation;
using Windows.Win32.System.Com;
using static Windows.Win32.PInvoke;

namespace VL.Video.MF
{
    // Good source: https://stackoverflow.com/questions/40913196/how-to-properly-use-a-hardware-accelerated-media-foundation-source-reader-to-dec
    [SupportedOSPlatform("windows8.0")]
    internal sealed unsafe class MFVideoPlayerImpl : VideoPlayerImpl
    {
        const CLSCTX clsctx = CLSCTX.CLSCTX_INPROC_SERVER | CLSCTX.CLSCTX_INPROC_HANDLER;

        private readonly IDisposable mf;
        private readonly VideoPlayer videoPlayer;
        private readonly ID3D11Device* device;
        private readonly IMFMediaEngine* engine;
        private readonly IWICImagingFactory* imagingFactory;

        private TexturePool? texturePool;
        private BitmapPool? bitmapPool;
        private Size2 renderTargetSize;

        private string? url;
        private Size2 textureSize;
        private bool playing, isEnded;
        private float duration, currentTime;
        private NetworkState networkState;
        private ReadyState readyState;

        public MFVideoPlayerImpl(VideoPlayer videoPlayer, IntPtr devicePtr)
        {
            // Initialize MediaFoundation
            mf = MediaFoundation.Use();

            this.videoPlayer = videoPlayer;

            if (devicePtr != default)
            {
                this.device = (ID3D11Device*)devicePtr;
                this.device->AddRef();
            }
            else
            {
                CoCreateInstance(in CLSID_WICImagingFactory1, null, clsctx, out IWICImagingFactory* imagingFactory).ThrowOnFailure();
                this.imagingFactory = imagingFactory;
            }

            IMFAttributes* mediaEngineAttributes;
            MFCreateAttributes(&mediaEngineAttributes, 1).ThrowOnFailure();
            mediaEngineAttributes->SetUINT32(in MF_MEDIA_ENGINE_AUDIO_CATEGORY, (uint)AUDIO_STREAM_CATEGORY.AudioCategory_GameMedia);
            mediaEngineAttributes->SetUINT32(in MF_MEDIA_ENGINE_AUDIO_ENDPOINT_ROLE, (uint)ERole.eMultimedia);
            // _SRGB doesn't work :/ Getting invalid argument exception later in TransferVideoFrame
            mediaEngineAttributes->SetUINT32(in MF_MEDIA_ENGINE_VIDEO_OUTPUT_FORMAT, (uint)DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM);

            if (device != null)
            {
                // Add multi thread protection on device (MF is multi-threaded)
                if (device->QueryInterface(in ID3D10Multithread.IID_Guid, out var x).Succeeded)
                    ((ID3D10Multithread*)x)->SetMultithreadProtected(true);

                // Reset device
                IMFDXGIDeviceManager* manager;
                MFCreateDXGIDeviceManager(out var resetToken, &manager).ThrowOnFailure();
                manager->ResetDevice((IUnknown*)(devicePtr), resetToken);
                mediaEngineAttributes->SetUnknown(in MF_MEDIA_ENGINE_DXGI_MANAGER, (IUnknown*)manager);
                manager->Release();
            }

            var notify = Notify.ComWrappers.GetOrCreateComInterfaceForObject(new Notify(this), CreateComInterfaceFlags.None);

            mediaEngineAttributes->SetUnknown(in MF_MEDIA_ENGINE_CALLBACK, (IUnknown*)notify);

            CoCreateInstance(in CLSID_MFMediaEngineClassFactory, null, clsctx, out IMFMediaEngineClassFactory* classFactory).ThrowOnFailure();

            IMFMediaEngine* engine;
            classFactory->CreateInstance(default, mediaEngineAttributes, &engine);
            this.engine = engine;

            classFactory->Release();
            mediaEngineAttributes->Release();
        }

        sealed class Notify : IMFMediaEngineNotify.Interface
        {
            public static readonly ComWrappers ComWrappers = new NotifyComWrappers();

            private readonly MFVideoPlayerImpl player;

            public Notify(MFVideoPlayerImpl player)
            {
                this.player = player;
            }

            public HRESULT EventNotify(uint @event, nuint param1, uint param2)
            {
                EventNotify((MF_MEDIA_ENGINE_EVENT)@event, param1, param2);
                return default;
            }

            private void EventNotify(MF_MEDIA_ENGINE_EVENT mediaEvent, nuint param1, uint param2)
            {
                var engine = player.engine;
                switch (mediaEvent)
                {
                    case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_LOADSTART:
                        player.playing = default;
                        player.isEnded = default;
                        player.duration = default;
                        player.currentTime = default;
                        player.ErrorCode = default;
                        break;
                    case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_ERROR:
                        player.ErrorCode = (ErrorState)param1;
                        break;
                    case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_LOADEDMETADATA:
                    case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_FORMATCHANGE:
                        player.renderTargetSize = default;
                        break;
                    case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_PLAYING:
                        player.playing = true;
                        break;
                    case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_PAUSE:
                        player.playing = false;
                        break;
                    case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_ENDED:
                        player.playing = false;
                        player.isEnded = true;
                        break;
                    case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_DURATIONCHANGE:
                        player.duration = (float)engine->GetDuration();
                        break;
                }

                player.networkState = (NetworkState)engine->GetNetworkState();
                player.readyState = (ReadyState)engine->GetReadyState();
            }

            class NotifyComWrappers : ComWrappers
            {
                protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
                {
                    // https://github.com/microsoft/CsWin32/issues/751#issuecomment-1304268295
                    var vtable = (IMFMediaEngineNotify.Vtbl*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IMFMediaEngineNotify), sizeof(IMFMediaEngineNotify.Vtbl)).ToPointer();

                    IUnknown.Vtbl* unknown = (IUnknown.Vtbl*)vtable;

                    GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease);
                    unknown->QueryInterface_1 = (delegate* unmanaged[Stdcall]<IUnknown*, Guid*, void**, HRESULT>)fpQueryInterface;
                    unknown->AddRef_2 = (delegate* unmanaged[Stdcall]<IUnknown*, uint>)fpAddRef;
                    unknown->Release_3 = (delegate* unmanaged[Stdcall]<IUnknown*, uint>)fpRelease;

                    IMFMediaEngineNotify.PopulateVTable(vtable);

                    var comInterfaceEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(NotifyComWrappers), sizeof(ComInterfaceEntry)).ToPointer();
                    comInterfaceEntry->IID = IMFMediaEngineNotify.IID_Guid;
                    comInterfaceEntry->Vtable = new IntPtr(vtable);
                    count = 1;
                    return comInterfaceEntry;
                }

                protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
                {
                    throw new NotImplementedException();
                }

                protected override void ReleaseObjects(IEnumerable objects)
                {
                    throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// Whether or not playback started.
        /// </summary>
        public override bool Playing => playing;

        /// <summary>
        /// A Boolean which is true if the media contained in the element has finished playing.
        /// </summary>
        public override bool IsEnded => isEnded;

        /// <summary>
        /// The current playback time in seconds
        /// </summary>
        public override float CurrentTime => currentTime;

        /// <summary>
        /// The length of the element's media in seconds.
        /// </summary>
        public override float Duration => duration;

        /// <summary>
        /// The current state of the fetching of media over the network.
        /// </summary>
        public override NetworkState NetworkState => networkState;

        /// <summary>
        /// The readiness state of the media.
        /// </summary>
        public override ReadyState ReadyState => readyState;

        /// <summary>
        /// Gets the most recent error status.
        /// </summary>
        public override ErrorState ErrorCode { get; protected set; }

        public override IResourceProvider<VideoFrame>? GrabVideoFrame()
        {
            // Synchronize parameters
            if (videoPlayer.Url != url)
            {
                url = videoPlayer.Url;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    fixed (char* c = url)
                        engine->SetSource(new BSTR(c)).ThrowOnFailure();
                }
            }

            if (videoPlayer.TextureSize != textureSize)
            {
                textureSize = videoPlayer.TextureSize;
                renderTargetSize = default;
            }

            engine->SetPlaybackRate(videoPlayer.Rate).ThrowOnFailure();
            engine->SetVolume(VLMath.Clamp(videoPlayer.Volume, 0f, 1f)).ThrowOnFailure();
            var currentTime = this.currentTime = (float)engine->GetCurrentTime();

            if (ReadyState <= ReadyState.HaveNothing)
            {
                renderTargetSize = default;
                return default;
            }

            if (ReadyState >= ReadyState.HaveMetadata)
            {
                if (videoPlayer.Seek)
                {
                    var seekTime = VLMath.Clamp(videoPlayer.SeekTime, 0, Duration);
                    engine->SetCurrentTime(seekTime).ThrowOnFailure();
                }

                engine->SetLoop(videoPlayer.Loop);
                if (videoPlayer.Loop)
                {
                    var loopStartTime = VLMath.Clamp(videoPlayer.LoopStartTime, 0f, Duration);
                    var loopEndTime = VLMath.Clamp(videoPlayer.LoopEndTime < 0 ? float.MaxValue : videoPlayer.LoopEndTime, 0f, Duration);
                    if (currentTime < loopStartTime || currentTime > loopEndTime)
                    {
                        if (videoPlayer.Rate >= 0)
                            engine->SetCurrentTime(loopStartTime).ThrowOnFailure();
                        else
                            engine->SetCurrentTime(loopEndTime).ThrowOnFailure();
                    }
                }

                if (videoPlayer.Play && engine->IsPaused() && !engine->IsEnded() /* https://discourse.vvvv.org/t/vl-video-mediafoundation-toggling-play-at-the-end-of-a-video-resets-position/21050 */)
                    engine->Play().ThrowOnFailure();
                else if (!videoPlayer.Play && !engine->IsPaused())
                    engine->Pause().ThrowOnFailure();
            }

            if (ReadyState >= ReadyState.HaveCurrentData && engine->HasVideo() && engine->OnVideoStreamTick(out var presentationTimeTicks).Succeeded && presentationTimeTicks >= 0)
            {
                lastPresentTime = presentationTimeTicks;

                var renderTargetSize = this.renderTargetSize;
                if (renderTargetSize == default)
                {
                    texturePool?.Dispose();
                    texturePool = null;

                    uint width, height;
                    engine->GetNativeVideoSize(&width, &height).ThrowOnFailure();

                    // Apply user specified size
                    var x = textureSize;
                    if (x.Width > 0)
                        width = (uint)x.Width;
                    if (x.Height > 0)
                        height = (uint)x.Height;

                    this.renderTargetSize = renderTargetSize = new Size2((int)width, (int)height);

                    if (renderTargetSize != default)
                    {
                        if (device != null)
                        {
                            texturePool = TexturePool.Get(device, new()
                            {
                                Width = (uint)renderTargetSize.Width,
                                Height = (uint)renderTargetSize.Height,
                                ArraySize = 1,
                                BindFlags = D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET | D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE,
                                // _SRGB doesn't work :/ Getting invalid argument exception in TransferVideoFrame
                                Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                                MipLevels = 1,
                                SampleDesc = new DXGI_SAMPLE_DESC() { Count = 1, Quality = 0 },
                                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT
                            });
                        }
                        else
                        {
                            bitmapPool = BitmapPool.Get(imagingFactory,
                                (uint)renderTargetSize.Width,
                                (uint)renderTargetSize.Height,
                                GUID_WICPixelFormat32bppBGRA);
                        }
                    }
                }

                if (renderTargetSize != default)
                {
                    if (texturePool != null)
                    {
                        var texture = texturePool.Rent();

                        engine->TransferVideoFrame(
                            (IUnknown*)texture.NativePointer.ToPointer(),
                            ToVideoRect(videoPlayer.SourceBounds),
                            new RECT(0, 0, renderTargetSize.Width, renderTargetSize.Height),
                            ToRawColorBGRA(videoPlayer.BorderColor));

                        var videoFrame = new GpuVideoFrame<BgraPixel>(texture);
                        return ResourceProvider.Return(videoFrame, (texture, texturePool), x => x.texturePool.Return(x.texture));
                    }
                    else if (bitmapPool != null)
                    {
                        var bitmap = bitmapPool.Rent();

                        engine->TransferVideoFrame(
                            (IUnknown*)bitmap,
                            ToVideoRect(videoPlayer.SourceBounds),
                            new RECT(0, 0, renderTargetSize.Width, renderTargetSize.Height),
                            ToRawColorBGRA(videoPlayer.BorderColor));

                        IWICBitmapLock* bitmapLock;
                        bitmap->Lock(new WICRect() { Width = renderTargetSize.Width, Height = renderTargetSize.Height }, (uint)WICBitmapLockFlags.WICBitmapLockRead, &bitmapLock);
                        byte* dataPtr;
                        bitmapLock->GetDataPointer(out var bufferSize, &dataPtr);
                        bitmapLock->GetStride(out var stride);
                        var pitch = (int)stride - renderTargetSize.Width * sizeof(BgraPixel);
                        var memoryOwner = new UnmanagedMemoryManager<BgraPixel>(new IntPtr(dataPtr), (int)bufferSize);
                        var memory = memoryOwner.Memory.AsMemory2D(0, renderTargetSize.Height, renderTargetSize.Width, pitch);
                        var videoFrame = new VideoFrame<BgraPixel>(memory);
                        return ResourceProvider.Return(videoFrame,
                            (owner: (IDisposable)memoryOwner, bitmap: new IntPtr(bitmap), bitmapLock: new IntPtr(bitmapLock), pool: bitmapPool),
                            static x =>
                            {
                                x.owner.Dispose();
                                ((IWICBitmapLock*)x.bitmapLock)->Release();
                                x.pool.Return((IWICBitmap*)x.bitmap);
                            });
                    }
                }
            }

            return null;
        }

        long lastPresentTime = long.MinValue;

        static MFVideoNormalizedRect? ToVideoRect(RectangleF? rect)
        {
            if (rect.HasValue)
            {
                var x = rect.Value;
                return new MFVideoNormalizedRect()
                {
                    left = VLMath.Clamp(x.Left, 0f, 1f),
                    bottom = VLMath.Clamp(x.Bottom, 0f, 1f),
                    right = VLMath.Clamp(x.Right, 0f, 1f),
                    top = VLMath.Clamp(x.Top, 0f, 1f)
                };
            }
            return default;
        }

        static MFARGB? ToRawColorBGRA(Color4? color)
        {
            if (color.HasValue)
            {
                color.Value.ToBgra(out var r, out var g, out var b, out var a);
                return new MFARGB
                {
                    rgbBlue = b,
                    rgbGreen = g,
                    rgbRed = r,
                    rgbAlpha = a
                };
            }
            return default;
        }

        public override void Dispose()
        {
            engine->Shutdown().ThrowOnFailure();
            engine->Release();
            texturePool?.Dispose();

            if (imagingFactory != null)
                imagingFactory->Release();

            if (device != null)
                device->Release();

            mf.Dispose();

            base.Dispose();
        }
    }
}

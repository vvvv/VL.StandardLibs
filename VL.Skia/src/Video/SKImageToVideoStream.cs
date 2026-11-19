using CommunityToolkit.HighPerformance;
using SkiaSharp;
using Stride.Core;
using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Basics.Imaging;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using VL.Skia.Egl;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;

[assembly: ImportType(typeof(VL.Skia.Video.SKImageToVideoStream))]

namespace VL.Skia.Video
{
    /// <summary>
    /// Converts a Skia image into a video stream by reading the pixels back from the GPU asynchronously.
    /// Each call to Update produces one video frame.
    /// </summary>
    /// <remarks>
    /// The frame rate pins only label the produced frames, no frame rate conversion takes place.
    /// If changed, make sure the values match the main loop settings of vvvv.
    /// </remarks>
    [ProcessNode(Category = "Graphics.Skia.Video")]
    [System.Runtime.Versioning.SupportedOSPlatform("windows6.1")]
    public sealed unsafe class SKImageToVideoStream : IDisposable
    {
        record struct MetadataAndFrameRate(string Metadata, (int N, int D) FrameRate);

        private static readonly IRefCounter<SKImage> refCounter = RefCounting.GetRefCounter<SKImage>();

        private readonly SynchronizationContext synchronizationContext = SynchronizationContext.Current;
        private readonly Queue<(nint texture, MetadataAndFrameRate metadata)> textureDownloads = new();
        private readonly Subject<IResourceProvider<VideoFrame>> frames = new Subject<IResourceProvider<VideoFrame>>();
        private readonly SerialDisposable texturePoolSubscription = new SerialDisposable();
        private readonly RenderContextProvider renderContextProvider;
        private readonly VideoStream videoStream;

        // Nullable
        private RenderContext previousRenderContext;
        // The device of the render context (not ours), and the texture Skia draws into
        private ID3D11Device* device;
        private ID3D11Texture2D* renderTarget;
        private D3D11_TEXTURE2D_DESC renderTargetDescription;
        private EglSurface eglSurface;
        private SKSurface surface;

        public SKImageToVideoStream()
        {
            renderContextProvider = AppHost.Current.GetRenderContextProvider();
            videoStream = new VideoStream(frames);
        }

        /// <param name="input">The image to stream. If null, no frame is produced.</param>
        /// <param name="metadata">Optional metadata attached to each video frame.</param>
        /// <param name="frameRateN">Numerator of the frame rate. Must match the main loop settings of vvvv, no conversion is done.</param>
        /// <param name="frameRateD">Denominator of the frame rate. Must match the main loop settings of vvvv, no conversion is done.</param>
        /// <returns>The stream of video frames. Frames arrive with a delay of a few updates due to the asynchronous readback.</returns>
        [return: Pin(Name = "Output")]
        public VideoStream Update(
            SKImage input,
            string metadata, 
            int frameRateN = Constants.DefaultFrameRateN, 
            int frameRateD = Constants.DefaultFrameRateD)
        {
            if (input is null)
                return null;

            var renderContext = renderContextProvider.GetRenderContext();
            if (renderContext != previousRenderContext)
            {
                previousRenderContext = renderContext;

                device = renderContext.EglContext.Display.TryGetD3D11DeviceInterface(out var d3dDevice) ? d3dDevice : null;
            }

            using var _ = renderContext.MakeCurrent(forRendering: false);
            if (device != null)
                DownloadWithStagingTexture(renderContext, input, metadata, (frameRateN, frameRateD));
            else
                DownloadWithRasterImage(input, metadata, (frameRateN, frameRateD));

            return videoStream;
        }

        private void DownloadWithStagingTexture(RenderContext renderContext, SKImage skImage, string metadata, (int N, int D) frameRate)
        {
            // Fast path
            // - Create render texture
            // - Make Skia surface out of it
            // - Draw image into surface
            // - Create staging texture
            // - Copy render texture into staging texture

            var description = new D3D11_TEXTURE2D_DESC()
            {
                Width = (uint)skImage.Width,
                Height = (uint)skImage.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                SampleDesc = new DXGI_SAMPLE_DESC() { Count = 1, Quality = 0 },
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                CPUAccessFlags = 0,
                BindFlags = D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET | D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE,
                MiscFlags = D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_SHARED
            };

            var stagingDescription = description;
            stagingDescription.CPUAccessFlags = D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ;
            stagingDescription.BindFlags = 0;
            stagingDescription.Usage = D3D11_USAGE.D3D11_USAGE_STAGING;
            stagingDescription.MiscFlags = 0;

            var texturePool = GetTexturePool(device, stagingDescription);

            // Render into a temp target and request copy of it to staging texture
            {
                if (renderTarget is null || renderTargetDescription.Width != description.Width || renderTargetDescription.Height != description.Height)
                {
                    surface?.Dispose();
                    eglSurface?.Dispose();
                    ReleaseRenderTarget();

                    ID3D11Texture2D* texture;
                    device->CreateTexture2D(&description, null, &texture);
                    renderTarget = texture;
                    renderTargetDescription = description;

                    IDXGIResource* sharedResource;
                    renderTarget->QueryInterface(out sharedResource).ThrowOnFailure();
                    HANDLE sharedHandle;
                    try
                    {
                        sharedResource->GetSharedHandle(&sharedHandle);
                    }
                    finally
                    {
                        sharedResource->Release();
                    }
                    eglSurface = renderContext.EglContext.CreateSurfaceFromSharedHandle(skImage.Width, skImage.Height, sharedHandle);

                    // Setup a skia surface around the currently set render target
                    surface = CreateSkSurface(renderContext, eglSurface, skImage.Width, skImage.Height);
                }

                // Render
                var canvas = surface.Canvas;
                canvas.DrawImage(skImage, 0f, 0f);
                // The drawing reaches the device before the copy does: Skia submits it to ANGLE, ANGLE to the immediate context the copy goes to
                surface.Flush();
                NativeGles.glFlush();

                var stagingTexture = texturePool.Rent();
                using (var deviceContext = ImmediateContext())
                    deviceContext.Ptr->CopyResource((ID3D11Resource*)stagingTexture, (ID3D11Resource*)renderTarget);
                textureDownloads.Enqueue(((nint)stagingTexture, new MetadataAndFrameRate(metadata, frameRate)));
            }

            // Download recently staged
            {
                var (staged, stagedMetadata) = textureDownloads.Peek();
                var stagedTexture = (ID3D11Texture2D*)staged;
                var doNotWait = textureDownloads.Count <= 8 /* Under normal scenarios we shouldn't reach this limit */;
                D3D11_MAPPED_SUBRESOURCE data;
                HRESULT mapped;
                using (var deviceContext = ImmediateContext())
                    mapped = deviceContext.Ptr->Map((ID3D11Resource*)stagedTexture, 0, D3D11_MAP.D3D11_MAP_READ, doNotWait ? (uint)D3D11_MAP_FLAG.D3D11_MAP_FLAG_DO_NOT_WAIT : 0u, &data);
                if (mapped.Succeeded)
                {
                    // Dequeue
                    textureDownloads.Dequeue();

                    // Setup the new image resource
                    stagedTexture->GetDesc(out var stagedDescription);
                    var width = (int)stagedDescription.Width;
                    var height = (int)stagedDescription.Height;
                    // The rows of the mapped texture are RowPitch bytes apart; the pitch of the frame is the padding of a row in pixels
                    var memoryOwner = new UnmanagedMemoryManager<BgraPixel>((nint)data.pData, (int)data.RowPitch * height);
                    var pitch = (int)data.RowPitch / Unsafe.SizeOf<BgraPixel>() - width;
                    var videoFrame = new VideoFrame<BgraPixel>(memoryOwner.Memory.AsMemory2D(0, height, width, pitch), stagedMetadata.Metadata, FrameRate: stagedMetadata.FrameRate);
                    var videoFrameProvider = ResourceProvider.Return(videoFrame, ReleaseVideoFrame);
                    using (videoFrameProvider.GetHandle())
                    {
                        // Push it downstream
                        frames.OnNext(videoFrameProvider);
                    }

                    void ReleaseVideoFrame(VideoFrame i)
                    {
                        if (SynchronizationContext.Current != synchronizationContext)
                            synchronizationContext.Post(x => ReleaseVideoFrame((VideoFrame)x), i);
                        else
                        {
                            ((IDisposable)memoryOwner).Dispose();
                            if (device != null)
                            {
                                using var deviceContext = ImmediateContext();
                                deviceContext.Ptr->Unmap((ID3D11Resource*)staged, 0);
                            }
                            texturePool.Return((ID3D11Texture2D*)staged);
                        }
                    }
                }
            }

            SKSurface CreateSkSurface(RenderContext context, EglSurface eglSurface, int width, int height)
            {
                var eglContext = context.EglContext;

                var colorType = SKColorType.Bgra8888;

                uint textureId = 0u;
                NativeGles.glGenTextures(1, ref textureId);
                NativeGles.glBindTexture(NativeGles.GL_TEXTURE_2D, textureId);
                if (!NativeEgl.eglBindTexImage(eglContext.Display, eglSurface, NativeEgl.EGL_BACK_BUFFER))
                    throw new Exception("Failed to bind surface");

                uint fbo = 0u;
                NativeGles.glGenFramebuffers(1, ref fbo);
                NativeGles.glBindFramebuffer(NativeGles.GL_FRAMEBUFFER, fbo);
                glFramebufferTexture2D(NativeGles.GL_FRAMEBUFFER, NativeGles.GL_COLOR_ATTACHMENT0, NativeGles.GL_TEXTURE_2D, textureId, 0);

                NativeGles.glGetIntegerv(NativeGles.GL_FRAMEBUFFER_BINDING, out var framebuffer);
                NativeGles.glGetIntegerv(NativeGles.GL_STENCIL_BITS, out var stencil);
                NativeGles.glGetIntegerv(NativeGles.GL_SAMPLES, out var samples);
                var maxSamples = context.SkiaContext.GetMaxSurfaceSampleCount(colorType);
                if (samples > maxSamples)
                    samples = maxSamples;

                var glInfo = new GRGlFramebufferInfo(
                    fboId: (uint)framebuffer,
                    format: colorType.ToGlSizedFormat());

                using var renderTarget = new GRBackendRenderTarget(
                    width: width,
                    height: height,
                    sampleCount: samples,
                    stencilBits: stencil,
                    glInfo: glInfo);

                return SKSurface.Create(
                    context.SkiaContext,
                    renderTarget,
                    GRSurfaceOrigin.TopLeft,
                    colorType,
                    colorspace: SKColorSpace.CreateSrgb());
            }

            D3D11TexturePool GetTexturePool(ID3D11Device* graphicsDevice, in D3D11_TEXTURE2D_DESC description)
            {
                return D3D11TexturePool.Get(graphicsDevice, in description)
                    .Subscribe(texturePoolSubscription);
            }

            [DllImport("libGLESv2.dll")]
            static extern void glFramebufferTexture2D(uint target, uint attachment, uint textarget, uint texture, int level);
        }

        private ComPtr<ID3D11DeviceContext> ImmediateContext()
        {
            ID3D11DeviceContext* deviceContext;
            device->GetImmediateContext(&deviceContext);
            return deviceContext;
        }

        private void ReleaseRenderTarget()
        {
            if (renderTarget != null)
            {
                renderTarget->Release();
                renderTarget = null;
            }
        }

        private void DownloadWithRasterImage(SKImage skImage, string metadata, (int N, int D) frameRate)
        {
            // Slow path through ToRasterImage
            if (skImage is null)
                return;

            if (skImage.ColorType != SKColorType.Bgra8888)
                throw new ArgumentException($"The color type must be {SKColorType.Bgra8888}", nameof(skImage));

            var rasterImage = skImage.ToRasterImage(ensurePixelData: true);
            if (rasterImage is null)
                return;

            if (rasterImage != skImage)
                refCounter.Init(rasterImage);
            else
                refCounter.AddRef(rasterImage);

            var pixmap = rasterImage.PeekPixels();
            var memoryOwner = new UnmanagedMemoryManager<BgraPixel>(pixmap.GetPixels(), pixmap.BytesSize);
            var pitch = pixmap.RowBytes / pixmap.BytesPerPixel - pixmap.Width;
            var videoFrame = new VideoFrame<BgraPixel>(memoryOwner.Memory.AsMemory2D(0, pixmap.Height, pixmap.Width, pitch), metadata, FrameRate: frameRate);
            var videoFrameProvider = ResourceProvider.Return(videoFrame, (rasterImage, pixmap, (IDisposable)memoryOwner), static x =>
            {
                var (rasterImage, pixmap, memoryOwner) = x;
                memoryOwner.Dispose();
                pixmap.Dispose();
                refCounter.Release(rasterImage);
            });

            frames.OnNext(videoFrameProvider);
        }

        public void Dispose()
        {
            frames.Dispose();
            texturePoolSubscription.Dispose();

            while (textureDownloads.Count > 0)
                ((ID3D11Texture2D*)textureDownloads.Dequeue().texture)->Release();

            var renderContext = renderContextProvider.GetRenderContext();
            using var _ = renderContext.MakeCurrent(forRendering: false);
            surface?.Dispose();
            eglSurface?.Dispose();
            ReleaseRenderTarget();
            device = null;
        }
    }
}
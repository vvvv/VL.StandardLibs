using CommunityToolkit.HighPerformance;
using SharpDX.Direct3D11;
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
using VL.Lib.Basics.Imaging;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using VL.Skia.Egl;
using MapMode = SharpDX.Direct3D11.MapMode;

namespace VL.Skia.Video
{
    public sealed class SKImageToVideoStream : IDisposable
    {
        private static readonly IRefCounter<SKImage> refCounter = RefCounting.GetRefCounter<SKImage>();

        private readonly SynchronizationContext synchronizationContext = SynchronizationContext.Current;
        private readonly Queue<(Texture2D texture, string metadata)> textureDownloads = new Queue<(Texture2D texture, string metadata)>();
        private readonly Subject<IResourceProvider<VideoFrame>> frames = new Subject<IResourceProvider<VideoFrame>>();
        private readonly SerialDisposable texturePoolSubscription = new SerialDisposable();
        private readonly RenderContext renderContext;
        private readonly VideoStream videoStream;

        // Nullable
        private Device device;
        private Texture2D renderTarget;
        private EglSurface eglSurface;
        private SKSurface surface;

        public SKImageToVideoStream()
        {
            renderContext = RenderContext.ForCurrentThread();
            var eglContext = renderContext.EglContext;
            if (eglContext.Dislpay.TryGetD3D11Device(out var devicePtr))
                device = new Device(devicePtr);
            videoStream = new VideoStream(frames);
        }

        public VideoStream Update(SKImage image, string metadata)
        {
            if (image is null)
                return null;

            if (device != null)
                DownloadWithStagingTexture(image, metadata);
            else
                DownloadWithRasterImage(image, metadata);

            return videoStream;
        }

        private void DownloadWithStagingTexture(SKImage skImage, string metadata)
        {
            // Fast path
            // - Create render texture
            // - Make Skia surface out of it
            // - Draw image into surface
            // - Create staging texture
            // - Copy render texture into staging texture

            var description = new Texture2DDescription()
            {
                Width = skImage.Width,
                Height = skImage.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                OptionFlags = ResourceOptionFlags.Shared
            };

            var stagingDescription = description;
            stagingDescription.CpuAccessFlags = CpuAccessFlags.Read;
            stagingDescription.BindFlags = BindFlags.None;
            stagingDescription.Usage = ResourceUsage.Staging;
            stagingDescription.OptionFlags = ResourceOptionFlags.None;

            var texturePool = GetTexturePool(device, stagingDescription);

            // Render into a temp target and request copy of it to staging texture
            {
                if (renderTarget is null || renderTarget.Description.Width != description.Width || renderTarget.Description.Height != description.Height)
                {
                    surface?.Dispose();
                    eglSurface?.Dispose();
                    renderTarget?.Dispose();

                    renderTarget = new Texture2D(device, description);

                    //eglSurface = eglContext.CreateSurfaceFromClientBuffer(renderTarget.NativePointer);
                    using var sharedResource = renderTarget.QueryInterface<SharpDX.DXGI.Resource>();
                    eglSurface = renderContext.EglContext.CreateSurfaceFromSharedHandle(description.Width, description.Height, sharedResource.SharedHandle);

                    // Setup a skia surface around the currently set render target
                    surface = CreateSkSurface(renderContext, eglSurface, renderTarget);
                }

                // Render
                var canvas = surface.Canvas;
                canvas.DrawImage(skImage, 0f, 0f);

                var stagingTexture = texturePool.Rent();
                device.ImmediateContext.CopyResource(renderTarget, stagingTexture);
                textureDownloads.Enqueue((stagingTexture, metadata));
            }

            // Download recently staged
            {
                var (stagedTexture, stagedMetadata) = textureDownloads.Peek();
                var doNotWait = textureDownloads.Count <= 8 /* Under normal scenarios we shouldn't reach this limit */;
                var data = device.ImmediateContext.MapSubresource(stagedTexture, 0, MapMode.Read, doNotWait ? MapFlags.DoNotWait : MapFlags.None);
                if (!data.IsEmpty)
                {
                    // Dequeue
                    textureDownloads.Dequeue();

                    // Setup the new image resource
                    var stagedDescription = stagedTexture.Description;
                    var memoryOwner = new UnmanagedMemoryManager<BgraPixel>(data.DataPointer, data.SlicePitch);
                    var pitch = data.RowPitch - (stagedDescription.Width * Unsafe.SizeOf<BgraPixel>());
                    var videoFrame = new VideoFrame<BgraPixel>(memoryOwner.Memory.AsMemory2D(0, stagedDescription.Height, stagedDescription.Width, pitch), stagedMetadata);
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
                            device?.ImmediateContext.UnmapSubresource(stagedTexture, 0);
                            texturePool.Return(stagedTexture);
                        }
                    }
                }
            }

            SKSurface CreateSkSurface(RenderContext context, EglSurface eglSurface, Texture2D texture)
            {
                var eglContext = context.EglContext;

                var colorType = SKColorType.Bgra8888;

                uint textureId = 0u;
                NativeGles.glGenTextures(1, ref textureId);
                NativeGles.glBindTexture(NativeGles.GL_TEXTURE_2D, textureId);
                var result = NativeEgl.eglBindTexImage(eglContext.Dislpay, eglSurface, NativeEgl.EGL_BACK_BUFFER);
                if (result == 0)
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
                    width: texture.Description.Width,
                    height: texture.Description.Height,
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

            D3D11TexturePool GetTexturePool(Device graphicsDevice, in Texture2DDescription description )
            {
                return D3D11TexturePool.Get(graphicsDevice, in description)
                    .Subscribe(texturePoolSubscription);
            }

            [DllImport("libGLESv2.dll")]
            static extern void glFramebufferTexture2D(uint target, uint attachment, uint textarget, uint texture, int level);
        }

        private void DownloadWithRasterImage(SKImage skImage, string metadata)
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
            var pitch = pixmap.RowBytes - (pixmap.Width * pixmap.BytesPerPixel);
            var videoFrame = new VideoFrame<BgraPixel>(memoryOwner.Memory.AsMemory2D(0, pixmap.Height, pixmap.Width, pitch), metadata);
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
                textureDownloads.Dequeue().texture.Dispose();

            surface?.Dispose();
            eglSurface?.Dispose();
            renderTarget?.Dispose();
            device = null;

            renderContext.Dispose();
        }
    }
}

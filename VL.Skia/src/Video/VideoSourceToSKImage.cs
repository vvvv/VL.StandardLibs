#nullable enable
using SkiaSharp;
using System.Threading;
using VL.Core;
using VL.Core.Utils;
using VL.Lib.Animation;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using VL.Lib.Video;

namespace VL.Skia.Video
{
    public sealed class VideoSourceToSKImage : VideoSourceToImage<SKImage>
    {
        private readonly RenderContext renderContext;
        private readonly VideoPlaybackContext ctx;
        private readonly Thread? mainThread;

        public VideoSourceToSKImage()
        {
            renderContext = RenderContext.ForCurrentThread();

            var frameClock = AppHost.Current.Services.GetRequiredService<IFrameClock>();
            if (renderContext.EglContext.Dislpay.TryGetD3D11Device(out var d3dDevice))
            {
                ctx = new VideoPlaybackContext(frameClock, d3dDevice, GraphicsDeviceType.Direct3D11, renderContext.UseLinearColorspace);
                mainThread = Thread.CurrentThread;
            }
            else
            {
                ctx = new VideoPlaybackContext(frameClock);
            }
        }

        protected override VideoPlaybackContext Context => ctx;

        protected override void OnPush(IResourceProvider<VideoFrame> videoFrameProvider, bool mipmapped)
        {
            var handle = videoFrameProvider.GetHandle();
            var videoFrame = handle.Resource;
            if (videoFrame.IsTextureBacked && !videoFrame.HasAttachedSkImage() && ctx != null && Thread.CurrentThread != mainThread)
            {
                // Wrapping needs to happen on the main thread
                if (!workQueueForMainThread.TryAddSafe(() => ProduceImage(handle, mipmapped), millisecondsTimeout: 10))
                    handle.Dispose();
            }
            else
            {
                // Video source pushed a memory backed frame
                ProduceImage(handle, mipmapped);
            }

            void ProduceImage(IResourceHandle<VideoFrame> handle, bool mipmapped)
            {
                var imageHandle = handle.ToSkImage(renderContext, mipmapped).GetHandle();
                if (!resultQueue.TryAddSafe(imageHandle, millisecondsTimeout: 10))
                    imageHandle.Dispose();
            }
        }

        protected override void OnPull(IResourceProvider<VideoFrame>? videoFrameProvider, bool mipmapped)
        {
            var handle = videoFrameProvider?.GetHandle().ToSkImage(renderContext, mipmapped).GetHandle();
            if (handle != null && !resultQueue.TryAddSafe(handle))
                handle.Dispose();
        }

        public override void Dispose()
        {
            base.Dispose();

            renderContext.Dispose();
        }
    }
}
#nullable restore
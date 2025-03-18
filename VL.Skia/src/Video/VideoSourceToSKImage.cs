#nullable enable
using SkiaSharp;
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

        public VideoSourceToSKImage(NodeContext nodeContext)
        {
            renderContext = RenderContext.ForCurrentApp();

            var frameClock = AppHost.Current.Services.GetRequiredService<IFrameClock>();
            if (renderContext.EglContext.Display.TryGetD3D11Device(out var d3dDevice))
                ctx = new VideoPlaybackContext(frameClock, nodeContext.GetLogger(), d3dDevice, GraphicsDeviceType.Direct3D11, renderContext.UseLinearColorspace);
            else
                ctx = new VideoPlaybackContext(frameClock, nodeContext.GetLogger());
        }

        protected override VideoPlaybackContext Context => ctx;

        protected override void OnPush(IResourceProvider<VideoFrame> videoFrameProvider, bool mipmapped)
        {
            var handle = videoFrameProvider?.GetHandle().ToSkImage(renderContext, mipmapped).GetHandle();
            if (handle != null && !resultQueue.TryAddSafe(handle, millisecondsTimeout: 10))
                handle.Dispose();
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
        }
    }
}
#nullable restore
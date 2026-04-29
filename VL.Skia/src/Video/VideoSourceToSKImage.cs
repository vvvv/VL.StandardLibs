#nullable enable
using System;
using SkiaSharp;
using VL.Core;
using VL.Core.Utils;
using VL.Lib.Animation;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using VL.Lib.Video;
using VL.Skia.Egl;

namespace VL.Skia.Video
{
    public sealed class VideoSourceToSKImage : VideoSourceToImage<SKImage>
    {
        private readonly RenderContextProvider renderContextProvider;
        private readonly VideoPlaybackContext ctx;

        public VideoSourceToSKImage(NodeContext nodeContext)
        {
            var appHost = AppHost.Current;
            renderContextProvider = appHost.GetRenderContextProvider();

            var frameClock = appHost.Services.GetRequiredService<IFrameClock>();
            var renderContext = renderContextProvider.GetRenderContext();
            if (renderContext.EglContext.Display.TryGetD3D11Device(out var d3dDevice))
                ctx = new VideoPlaybackContext(frameClock, nodeContext.GetLogger(), GetGraphicsDevice, GraphicsDeviceType.Direct3D11, renderContext.UseLinearColorspace);
            else
                ctx = new VideoPlaybackContext(frameClock, nodeContext.GetLogger());

            IntPtr GetGraphicsDevice()
            {
                var renderContext = renderContextProvider.GetRenderContext();
                if (renderContext.EglContext.Display.TryGetD3D11Device(out var d3dDevice))
                    return d3dDevice;
                return IntPtr.Zero;
            }
        }

        protected override VideoPlaybackContext Context => ctx;

        protected override IResourceHandle<SKImage?>? GetHandle(IResourceProvider<VideoFrame> videoFrameProvider, bool mipmapped)
        {
            return videoFrameProvider.ToSkImage(renderContextProvider, mipmapped).GetHandle();
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
#nullable restore
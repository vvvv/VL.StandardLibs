#nullable enable
using Stride.Core;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using VL.Core;
using VL.Core.Utils;
using VL.Lib.Animation;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using VL.Lib.Video;

namespace VL.Stride.Video
{
    public sealed class VideoSourceToTexture : VideoSourceToImage<Texture>
    {
        private readonly AppHost appHost;
        private readonly RenderContext renderContext;
        private readonly VideoPlaybackContext ctx;

        public VideoSourceToTexture(NodeContext nodeContext)
        {
            appHost = AppHost.Current;
            renderContext = RenderContext.GetShared(appHost.Services.GetRequiredService<Game>().Services);

            var graphicsDevice = renderContext.GraphicsDevice;
            var frameClock = AppHost.Current.Services.GetRequiredService<IFrameClock>();
            if (SharpDXInterop.GetNativeDevice(graphicsDevice) is SharpDX.Direct3D11.Device device)
                ctx = new VideoPlaybackContext(frameClock, nodeContext.GetLogger(), device.NativePointer, GraphicsDeviceType.Direct3D11, graphicsDevice.ColorSpace == ColorSpace.Linear);
            else
                ctx = new VideoPlaybackContext(frameClock, nodeContext.GetLogger());
        }

        protected override VideoPlaybackContext Context => ctx;

        protected override void OnPush(IResourceProvider<VideoFrame> videoFrameProvider, bool mipmapped)
        {
            using var _ = appHost.MakeCurrent();
            var handle = videoFrameProvider?.ToTexture(renderContext).GetHandle();
            if (handle != null && !resultQueue.TryAddSafe(handle, millisecondsTimeout: 10))
                handle.Dispose();
        }

        protected override void OnPull(IResourceProvider<VideoFrame>? videoFrameProvider, bool mipmapped)
        {
            var handle = videoFrameProvider?.ToTexture(renderContext).GetHandle();
            if (handle != null && !resultQueue.TryAddSafe(handle, millisecondsTimeout: 10))
                handle.Dispose();
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
#nullable restore
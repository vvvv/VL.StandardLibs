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
        private readonly IResourceHandle<RenderContext> renderContextHandle;
        private readonly VideoPlaybackContext ctx;

        public VideoSourceToTexture()
        {
            renderContextHandle = AppHost.Current.Services.GetGameProvider()
                .Bind(g => RenderContext.GetShared(g.Services))
                .GetHandle() ?? throw new ServiceNotFoundException(typeof(IResourceProvider<Game>));

            var graphicsDevice = renderContextHandle.Resource.GraphicsDevice;
            var frameClock = AppHost.Current.Services.GetRequiredService<IFrameClock>();
            if (SharpDXInterop.GetNativeDevice(graphicsDevice) is SharpDX.Direct3D11.Device device)
                ctx = new VideoPlaybackContext(frameClock, device.NativePointer, GraphicsDeviceType.Direct3D11, graphicsDevice.ColorSpace == ColorSpace.Linear);
            else
                ctx = new VideoPlaybackContext(frameClock);
        }

        protected override VideoPlaybackContext Context => ctx;

        protected override void OnPush(IResourceProvider<VideoFrame> videoFrameProvider, bool mipmapped)
        {
            var renderDrawContext = renderContextHandle.Resource.GetThreadContext();
            var handle = videoFrameProvider?.ToTexture(renderDrawContext).GetHandle();
            if (handle != null && !resultQueue.TryAddSafe(handle, millisecondsTimeout: 10))
                handle.Dispose();
        }

        protected override void OnPull(IResourceProvider<VideoFrame>? videoFrameProvider, bool mipmapped)
        {
            var renderDrawContext = renderContextHandle.Resource.GetThreadContext();
            var handle = videoFrameProvider?.ToTexture(renderDrawContext).GetHandle();
            if (handle != null && !resultQueue.TryAddSafe(handle, millisecondsTimeout: 10))
                handle.Dispose();
        }

        public override void Dispose()
        {
            base.Dispose();

            renderContextHandle.Dispose();
        }
    }
}
#nullable restore
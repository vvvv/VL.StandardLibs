#nullable enable
using Stride.Core;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using VL.Core;
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
            ctx = VideoUtils.CreatePlaybackContext(nodeContext);
        }

        protected override VideoPlaybackContext Context => ctx;

        protected override IResourceHandle<Texture?>? GetHandle(IResourceProvider<VideoFrame> videoFrameProvider, bool mipmapped)
        {
            return videoFrameProvider.ToTexture(renderContext).GetHandle();
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
#nullable restore
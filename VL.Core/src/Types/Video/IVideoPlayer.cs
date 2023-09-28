#nullable enable
using System;
using VL.Lib.Basics.Resources;

namespace VL.Lib.Basics.Video
{
    public interface IVideoPlayer : IDisposable
    {
        IResourceProvider<VideoFrame>? GrabVideoFrame();
    }
}
#nullable restore
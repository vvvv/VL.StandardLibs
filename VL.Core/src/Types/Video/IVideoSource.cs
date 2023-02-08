#nullable enable
using VL.Lib.Basics.Resources;

namespace VL.Lib.Basics.Video
{
    public interface IVideoSource
    {
        /// <summary>
        /// Grabs the next video frame from the source.
        /// </summary>
        IResourceProvider<VideoFrame> GrabVideoFrame();
    }
}
#nullable restore
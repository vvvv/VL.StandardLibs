#nullable enable
using VL.Lib.Basics.Resources;

namespace VL.Lib.Basics.Video
{
    /// <summary>
    /// This interface should no longer be implemented. Implement <see cref="IVideoSource2"/> instead.
    /// </summary>
    public interface IVideoSource
    {
        /// <summary>
        /// Grabs the next video frame from the source.
        /// </summary>
        IResourceProvider<VideoFrame>? GrabVideoFrame() => null;
    }
}
#nullable restore
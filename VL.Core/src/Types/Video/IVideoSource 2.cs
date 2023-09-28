#nullable enable
namespace VL.Lib.Basics.Video
{
    public interface IVideoSource2 : IVideoSource
    {
        /// <summary>
        /// Starts a new video playback. Must be thread safe.
        /// </summary>
        IVideoPlayer? Start(VideoPlaybackContext ctx);

        /// <summary>
        /// Sinks observe this property to determine whether they need to re-subscribe on the source.
        /// </summary>
        int ChangedTicket => 0;
    }
}
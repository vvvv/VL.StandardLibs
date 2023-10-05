#nullable enable

namespace VL.Video
{
    public enum NetworkState : short
    {
        /// <summary>
        /// There is no data yet. Also, readyState is HaveNothing.
        /// </summary>
        Empty,
        /// <summary>
        /// HTMLMediaElement is active and has selected a resource, but is not using the network.
        /// </summary>
        Idle,
        /// <summary>
        /// The browser is downloading HTMLMediaElement data.
        /// </summary>
        Loading,
        /// <summary>
        /// No HTMLMediaElement src found.
        /// </summary>
        NoSource
    }
}

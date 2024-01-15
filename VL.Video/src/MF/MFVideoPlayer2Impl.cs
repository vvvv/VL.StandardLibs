#nullable enable
using System;
using System.Runtime.Versioning;
using VL.Lib.Animation;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using Windows.Win32.Graphics.Direct3D11;

namespace VL.Video.MF
{
    // TODO: Not finished
    [SupportedOSPlatform("windows6.1")]
    internal sealed unsafe class MFVideoPlayer2Impl : VideoPlayerImpl
    {
        private readonly SourceReader sourceReader;
        private readonly IFrameClock frameClock;

        public MFVideoPlayer2Impl(IFrameClock frameClock, string url, IntPtr device)
        {
            this.frameClock = frameClock;

            sourceReader = SourceReader.CreateFromUrl(url, (ID3D11Device*)device, readAsync: false /* Only needed for devices */);
        }

        /// <summary>
        /// Whether or not playback started.
        /// </summary>
        public override bool Playing => default;

        /// <summary>
        /// A Boolean which is true if the media contained in the element has finished playing.
        /// </summary>
        public override bool IsEnded => default;

        /// <summary>
        /// The current playback time in seconds
        /// </summary>
        public override float CurrentTime => start != null ? (float)(Time.ToTimeSpan(frameClock.Time) - start.Value).TotalSeconds : 0f;

        /// <summary>
        /// The length of the element's media in seconds.
        /// </summary>
        public override float Duration => default;

        /// <summary>
        /// The current state of the fetching of media over the network.
        /// </summary>
        public override NetworkState NetworkState => default;

        /// <summary>
        /// The readiness state of the media.
        /// </summary>
        public override ReadyState ReadyState => default;

        /// <summary>
        /// Gets the most recent error status.
        /// </summary>
        public override ErrorState ErrorCode { get; protected set; }

        public override IResourceProvider<VideoFrame>? GrabVideoFrame()
        {
            var currentTime = Time.ToTimeSpan(frameClock.Time);
            if (start is null)
                start = currentTime;

            var currentTimeInStream = currentTime - start;
            while (currentFrameHandle is null || currentFrameHandle.Resource.Timecode < currentTimeInStream)
            {
                currentFrameHandle?.Dispose();
                currentFrame = sourceReader.GrabVideoFrame();
                currentFrameHandle = currentFrame?.GetHandle();

                if (currentFrame is null)
                    break;
            }

            return currentFrame;
        }

        IResourceProvider<VideoFrame>? currentFrame;
        IResourceHandle<VideoFrame>? currentFrameHandle;
        TimeSpan? start;

        public override void Dispose()
        {
            currentFrameHandle?.Dispose();
            sourceReader.Dispose();

            base.Dispose();
        }
    }
}

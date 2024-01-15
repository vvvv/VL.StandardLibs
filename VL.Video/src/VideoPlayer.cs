#nullable enable
using Stride.Core.Mathematics;
using System;
using VL.Lib.Basics.Video;
using System.Threading;

namespace VL.Video
{
    public sealed partial class VideoPlayer : IVideoSource2
    {
        private VideoPlayerImpl? currentPlayer;

        /// <summary>
        /// The URL of the media to play.
        /// </summary>
        public string? Url { internal get; set; }

        /// <summary>
        /// Set to true to start playback, false to pause playback.
        /// </summary>
        public bool Play { internal get; set; }

        /// <summary>
        /// Gets or sets the rate at which the media is being played back.
        /// </summary>
        public float Rate { get; set; } = 1f;

        public float SeekTime { get; set; }

        public bool Seek { get; set; }

        public float LoopStartTime { get; set; }

        public float LoopEndTime { get; set; } = float.MaxValue;

        public bool Loop { get; set; }

        /// <summary>
        /// The audio volume.
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// The normalized source rectangle.
        /// </summary>
        public RectangleF? SourceBounds { internal get; set; }

        /// <summary>
        /// The border color.
        /// </summary>
        public Color4? BorderColor { internal get; set; }

        /// <summary>
        /// The size of the output texture. Use zero to take the size from the video.
        /// </summary>
        public Size2 TextureSize { internal get; set; }

        /// <summary>
        /// Whether or not playback started.
        /// </summary>
        public bool Playing => currentPlayer?.Playing ?? false;

        /// <summary>
        /// A Boolean which is true if the media contained in the element has finished playing.
        /// </summary>
        public bool IsEnded => currentPlayer?.IsEnded ?? false;

        /// <summary>
        /// The current playback time in seconds
        /// </summary>
        public float CurrentTime => currentPlayer?.CurrentTime ?? default;

        /// <summary>
        /// The length of the element's media in seconds.
        /// </summary>
        public float Duration => currentPlayer?.Duration ?? default;

        /// <summary>
        /// The current state of the fetching of media over the network.
        /// </summary>
        public NetworkState NetworkState => currentPlayer?.NetworkState ?? default;

        /// <summary>
        /// The readiness state of the media.
        /// </summary>
        public ReadyState ReadyState => currentPlayer?.ReadyState ?? default;

        /// <summary>
        /// Gets the most recent error status.
        /// </summary>
        public ErrorState ErrorCode => currentPlayer?.ErrorCode ?? default;

        // This method is not really needed but makes it simpler to work with inside VL
        public IVideoSource Update(
            string url,
            bool play = false,
            float rate = 1f,
            float seekTime = 0f,
            bool seek = false,
            float loopStartTime = 0f,
            float loopEndTime = -1f,
            bool loop = false,
            float volume = 1f,
            Int2 textureSize = default,
            RectangleF? sourceBounds = default,
            Color4? borderColor = default)
        {
            Url = url;
            Play = play;
            Rate = rate;
            SeekTime = seekTime;
            Seek = seek;
            LoopStartTime = loopStartTime;
            LoopEndTime = loopEndTime;
            Loop = loop;
            Volume = volume;
            TextureSize = new Size2(textureSize.X, textureSize.Y);
            SourceBounds = sourceBounds;
            BorderColor = borderColor;

            return this;
        }

        IVideoPlayer? IVideoSource2.Start(VideoPlaybackContext ctx)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(8))
            {
                var devicePtr = ctx.GraphicsDeviceType == GraphicsDeviceType.Direct3D11 ? ctx.GraphicsDevice : default;
                var player = new MF.MFVideoPlayerImpl(this, devicePtr);

                // TODO: Reads the file frame by frame. Nice! We need to explore this more.
                //var player = new MF.MFVideoPlayer2Impl(ctx.FrameClock, Url, devicePtr);

                var previousPlayer = Interlocked.Exchange(ref currentPlayer, player);
                player.DisposeAction = () =>
                {
                    Interlocked.CompareExchange(ref currentPlayer, previousPlayer, player);
                };

                return player;
            }

            throw new PlatformNotSupportedException();
        }
    }
}

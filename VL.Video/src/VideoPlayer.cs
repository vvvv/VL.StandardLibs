#nullable enable
using Stride.Core.Mathematics;
using System;
using System.ComponentModel;
using System.Threading;
using VL.Core.Import;
using VL.Lib.Basics.Video;
using VL.Model;

namespace VL.Video
{
    [ProcessNode(Name = "VideoPlayer (Url)", Category = "Video", FragmentSelection = FragmentSelection.Explicit, Summary = "Play videos from a given web url", Tags = "web,avi,wmv,mp4,h264,mjpeg,mpeg,dv,mov")]
    public sealed partial class VideoPlayer : IVideoSource2
    {
        private readonly object syncRoot = new();
        private VideoPlayerImpl? currentPlayer;
        private int changedTicket;
        private bool useLinearTextureFormat;

        [Fragment]
        public VideoPlayer()
        {
        }

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
            Color4? borderColor = default,
            bool useLinearTextureFormat = false)
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
            if (useLinearTextureFormat != this.useLinearTextureFormat)
            {
                this.useLinearTextureFormat = useLinearTextureFormat;
                changedTicket++;
            }

            return this;
        }

        /// <summary>Configures playback and exposes the current player status.</summary>
        /// <param name="url">URL of the media to play.</param>
        /// <param name="play">Whether playback is active.</param>
        /// <param name="rate">Playback speed multiplier: e.g: 1.0 = normal speed, 0.5 = half speed, 2.0 = double speed</param>
        /// <param name="seekTime">Target playback position in seconds.</param>
        /// <param name="seek">Whether to seek to the specified position.</param>
        /// <param name="loopStartTime">Start of the loop in seconds.</param>
        /// <param name="loopEndTime">End of the loop in seconds.</param>
        /// <param name="loop">Whether looping is enabled.</param>
        /// <param name="volume">Audio volume.</param>
        /// <param name="textureSize">Output texture size; zero uses the source dimensions.</param>
        /// <param name="sourceBounds">Normalized source rectangle.</param>
        /// <param name="borderColor">Border color.</param>
        /// <param name="useLinearTextureFormat">Whether to use a linear texture format.</param>
        /// <param name="playing">Whether playback started.</param>
        /// <param name="currentTime">Current playback time in seconds.</param>
        /// <param name="duration">Media duration in seconds.</param>
        /// <param name="readyState">Readiness state of the media.</param>
        /// <param name="networkState">Current network loading state.</param>
        /// <param name="errorCode">Most recent error status.</param>
        [Fragment]
        [return: Pin(Name = "Output")]
        public IVideoSource Update(
            [Pin(Name = "Url"), DefaultValue("")] string? url,
            [Pin(Name = "Play"), DefaultValue(false)] bool play,
            [Pin(Name = "Rate"), DefaultValue(1f)] float rate,
            [Pin(Name = "Seek Time"), DefaultValue(0f)] float seekTime,
            [Pin(Name = "Seek"), DefaultValue(false)] bool seek,
            [Pin(Name = "Loop Start Time"), DefaultValue(0f)] float loopStartTime,
            [Pin(Name = "Loop End Time"), DefaultValue(-1f)] float loopEndTime,
            [Pin(Name = "Loop"), DefaultValue(false)] bool loop,
            [Pin(Name = "Volume"), DefaultValue(1f)] float volume,
            [Pin(Name = "Texture Size", Visibility = PinVisibility.Optional)] Int2 textureSize,
            [Pin(Name = "Source Bounds", Visibility = PinVisibility.Optional)] RectangleF? sourceBounds,
            [Pin(Name = "Border Color", Visibility = PinVisibility.Optional)] Color4? borderColor,
            [Pin(Name = "Use Linear Texture Format", Visibility = PinVisibility.Optional), DefaultValue(false)] bool useLinearTextureFormat,
            [Pin(Name = "Playing")] out bool playing,
            [Pin(Name = "Current Time")] out float currentTime,
            [Pin(Name = "Duration")] out float duration,
            [Pin(Name = "Ready State")] out ReadyState readyState,
            [Pin(Name = "Network State")] out NetworkState networkState,
            [Pin(Name = "Error Code")] out ErrorState errorCode)
        {
            var output = Update(url ?? string.Empty, play, rate, seekTime, seek, loopStartTime, loopEndTime, loop, volume, textureSize, sourceBounds, borderColor, useLinearTextureFormat);
            playing = Playing;
            currentTime = CurrentTime;
            duration = Duration;
            readyState = ReadyState;
            networkState = NetworkState;
            errorCode = ErrorCode;
            return output;
        }

        IVideoPlayer? IVideoSource2.Start(VideoPlaybackContext ctx)
        {
            lock (syncRoot)
            {
                if (currentPlayer != null)
                    return null;

                if (OperatingSystem.IsWindowsVersionAtLeast(8))
                {
                    var devicePtr = ctx.GraphicsDeviceType == GraphicsDeviceType.Direct3D11 ? ctx.GraphicsDevice : default;
                    return currentPlayer = new MF.MFVideoPlayerImpl(this, devicePtr, ctx.UsesLinearColorspace && useLinearTextureFormat)
                    {
                        DisposeAction = () =>
                        {
                            currentPlayer = null;
                            // Tell another sink that it can try to subscribe again
                            changedTicket++;
                        }
                    };

                    // TODO: Reads the file frame by frame. Nice! We need to explore this more.
                    //var player = new MF.MFVideoPlayer2Impl(ctx.FrameClock, Url, devicePtr);
                }

                throw new PlatformNotSupportedException();
            }
        }

        int IVideoSource2.ChangedTicket => changedTicket;
    }
}

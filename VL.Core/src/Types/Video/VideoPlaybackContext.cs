#nullable enable
using Microsoft.Extensions.Logging;
using System;
using VL.Core;
using VL.Lib.Animation;

namespace VL.Lib.Basics.Video
{
    public sealed class VideoPlaybackContext
    {
        // 6.0 compatibility
        public VideoPlaybackContext(IFrameClock frameClock, IntPtr graphicsDevice = default, GraphicsDeviceType deviceType = default, bool usesLinearColorspace = false)
            : this(frameClock, AppHost.Current.LoggerFactory.CreateLogger<VideoPlaybackContext>(), graphicsDevice, deviceType, usesLinearColorspace)
        {

        }

        public VideoPlaybackContext(IFrameClock frameClock, ILogger logger, IntPtr graphicsDevice = default, GraphicsDeviceType deviceType = default, bool usesLinearColorspace = false)
        {
            FrameClock = frameClock;
            Logger = logger;
            GraphicsDevice = graphicsDevice;
            GraphicsDeviceType = deviceType;
            UsesLinearColorspace = usesLinearColorspace;
        }

        public IFrameClock FrameClock { get; }
        public ILogger Logger { get; }

        public IntPtr GraphicsDevice { get; }

        public GraphicsDeviceType GraphicsDeviceType { get; }

        public bool UsesLinearColorspace { get; }
    }
}

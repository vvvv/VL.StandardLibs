#nullable enable
using Microsoft.Extensions.Logging;
using System;
using VL.Core;
using VL.Lib.Animation;

namespace VL.Lib.Basics.Video
{
    public sealed class VideoPlaybackContext
    {
        private readonly Func<IntPtr>? graphicsDeviceFactory;

        // 6.0 compatibility
        public VideoPlaybackContext(IFrameClock frameClock, IntPtr graphicsDevice = default, GraphicsDeviceType deviceType = default, bool usesLinearColorspace = false)
            : this(frameClock, AppHost.Current.LoggerFactory.CreateLogger<VideoPlaybackContext>(), graphicsDevice != default ? () => graphicsDevice : default, deviceType, usesLinearColorspace)
        {

        }

        public VideoPlaybackContext(IFrameClock frameClock, ILogger logger, Func<IntPtr>? graphicsDeviceFactory = default, GraphicsDeviceType deviceType = default, bool usesLinearColorspace = false)
        {
            FrameClock = frameClock;
            Logger = logger;
            this.graphicsDeviceFactory = graphicsDeviceFactory;
            GraphicsDeviceType = deviceType;
            UsesLinearColorspace = usesLinearColorspace;
        }

        public IFrameClock FrameClock { get; }
        public ILogger Logger { get; }

        public IntPtr GraphicsDevice => graphicsDeviceFactory != null ? graphicsDeviceFactory.Invoke() : IntPtr.Zero;

        public GraphicsDeviceType GraphicsDeviceType { get; }

        public bool UsesLinearColorspace { get; }
    }
}

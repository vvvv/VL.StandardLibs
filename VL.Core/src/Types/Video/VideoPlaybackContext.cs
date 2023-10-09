#nullable enable
using System;
using VL.Lib.Animation;

namespace VL.Lib.Basics.Video
{
    public sealed class VideoPlaybackContext
    {
        public VideoPlaybackContext(IFrameClock frameClock, IntPtr graphicsDevice = default, GraphicsDeviceType deviceType = default, bool usesLinearColorspace = false)
        {
            FrameClock = frameClock;
            GraphicsDevice = graphicsDevice;
            GraphicsDeviceType = deviceType;
            UsesLinearColorspace = usesLinearColorspace;
        }

        public IFrameClock FrameClock { get; }

        public IntPtr GraphicsDevice { get; }

        public GraphicsDeviceType GraphicsDeviceType { get; }

        public bool UsesLinearColorspace { get; }
    }
}

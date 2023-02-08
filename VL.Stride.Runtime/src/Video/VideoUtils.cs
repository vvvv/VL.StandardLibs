using Stride.Graphics;
using System;
using VL.Lib.Basics.Video;
using VL.Stride.Utils;

namespace VL.Stride.Video
{
    // Used by VideoSourceToTexture patch
    public static class VideoUtils
    {
        public static unsafe Texture ToTexture(VideoFrame videoFrame, GraphicsDevice graphicsDevice)
        {
            if (!videoFrame.TryGetMemory(out var memory))
                return null;

            fixed (byte* data = memory.Span)
            {
                var description = TextureDescription.New2D(videoFrame.Width, videoFrame.Height, ImageUtils.ToPixelFormat(videoFrame.PixelFormat), usage: GraphicsResourceUsage.Immutable);
                return Texture.New(graphicsDevice, description, new DataBox(new IntPtr(data), videoFrame.RowLengthInBytes, videoFrame.LengthInBytes));
            }
        }
    }
}

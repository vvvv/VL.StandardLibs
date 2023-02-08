using SkiaSharp;
using System;
using VL.Lib.Basics.Video;

namespace VL.Skia.Video
{
    // Used by VideoSourceToSKImage
    public static class VideoUtils
    {
        public static unsafe SKImage ToSkImage(VideoFrame videoFrame)
        {
            if (!videoFrame.TryGetMemory(out var memory))
                throw new NotImplementedException();

            var skInfo = new SKImageInfo(videoFrame.Width, videoFrame.Height, videoFrame.PixelFormat.ToSkColorType(), SKAlphaType.Unpremul);
            var handle = memory.Pin();
            var pixmap = new SKPixmap(skInfo, new IntPtr(handle.Pointer), videoFrame.RowLengthInBytes);
            return SKImage.FromPixels(pixmap, (a, b) =>
            {
                pixmap.Dispose();
                handle.Dispose();
            });
        }

        public static unsafe SKImage ToSkImage(VideoFrame videoFrame, GRContext context, bool mipmapped)
        {
            using var rasterImage = ToSkImage(videoFrame);
            return rasterImage.ToTextureImage(context, mipmapped);
        }
    }
}

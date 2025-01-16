#nullable enable
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public record struct R16Pixel(byte R) : IPixel
    {
        public PixelFormat PixelFormat => PixelFormat.R16;
    }
}
#nullable restore
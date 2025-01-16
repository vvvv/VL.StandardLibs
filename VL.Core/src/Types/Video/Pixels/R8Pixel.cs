#nullable enable
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public record struct R8Pixel(byte R) : IPixel
    {
        public PixelFormat PixelFormat => PixelFormat.R8;
    }
}
#nullable restore
#nullable enable
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public record struct R32fPixel(float R) : IPixel
    {
        public PixelFormat PixelFormat => PixelFormat.R32F;
    }
}
#nullable restore
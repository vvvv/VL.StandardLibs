#nullable enable
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public record struct RgbaPixel(byte R, byte G, byte B, byte A) : IPixel
    {
        public PixelFormat PixelFormat => PixelFormat.R8G8B8A8;
    }
}
#nullable restore
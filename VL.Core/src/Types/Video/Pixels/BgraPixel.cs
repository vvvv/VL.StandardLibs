#nullable enable
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public record struct BgraPixel(byte B, byte G, byte R, byte A) : IPixel
    {
        public PixelFormat PixelFormat => PixelFormat.B8G8R8A8;
    }
}
#nullable restore
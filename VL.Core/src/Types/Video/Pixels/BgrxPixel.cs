#nullable enable
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public record struct BgrxPixel(byte B, byte G, byte R, byte X) : IPixel
    {
        public PixelFormat PixelFormat => PixelFormat.B8G8R8X8;
    }
}
#nullable restore
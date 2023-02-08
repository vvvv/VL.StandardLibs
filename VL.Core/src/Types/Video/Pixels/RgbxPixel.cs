#nullable enable
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public record struct RgbxPixel(byte R, byte G, byte B, byte X) : IPixel
    {
        public PixelFormat PixelFormat => PixelFormat.R8G8B8X8;
    }
}
#nullable restore
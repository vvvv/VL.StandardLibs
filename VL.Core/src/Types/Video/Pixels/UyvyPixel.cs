#nullable enable
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    /// <summary>
    /// Represents a UYVY macro pixel containing 2 pixels worth of data (4 bytes).
    /// UYVY is a packed 4:2:2 YUV format where U and V are shared between 2 Y samples.
    /// Byte order: U0, Y0, V0, Y1 (4 bytes for 2 pixels).
    /// </summary>
    public record struct UyvyPixel(byte U, byte Y0, byte V, byte Y1) : IPixel
    {
        public PixelFormat PixelFormat => PixelFormat.UYVY;
    }
}
#nullable restore

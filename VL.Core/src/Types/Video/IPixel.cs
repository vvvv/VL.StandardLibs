#nullable enable
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public interface IPixel
    {
        PixelFormat PixelFormat { get; }
    }
}
#nullable restore
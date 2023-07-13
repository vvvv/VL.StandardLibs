#nullable enable
using System;
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public record struct Rgba16fPixel(Half R, Half G, Half B, Half A) : IPixel
    {
        public PixelFormat PixelFormat => PixelFormat.R16G16B16A16F;
    }
}
#nullable restore
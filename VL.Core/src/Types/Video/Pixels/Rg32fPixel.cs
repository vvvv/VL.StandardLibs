#nullable enable
using System;
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public record struct Rg32fPixel(float R, float G) : IPixel
    {
        public PixelFormat PixelFormat => PixelFormat.R32G32F;
    }
}
#nullable restore
#nullable enable
using System;
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public record struct Rgba32fPixel(float R, float G, float B, float A) : IPixel
    {
        public PixelFormat PixelFormat => PixelFormat.R32G32B32A32F;
    }
}
#nullable restore
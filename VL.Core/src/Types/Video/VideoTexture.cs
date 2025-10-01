#nullable enable
using System;
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public readonly record struct VideoTexture(IntPtr nativePointer, int width, int height, PixelFormat pixelFormat)
    {
        public IntPtr NativePointer => nativePointer;

        public int Width => width;

        public int Height => height;

        public PixelFormat PixelFormat => pixelFormat;
    }
}

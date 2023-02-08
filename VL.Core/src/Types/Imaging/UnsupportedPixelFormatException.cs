using System;

namespace VL.Lib.Basics.Imaging
{
    public class UnsupportedPixelFormatException : Exception
    {
        public UnsupportedPixelFormatException(PixelFormat format) 
            : base($"Unsupported pixel format {format}")
        {
            Format = format;
        }

        public PixelFormat Format { get; }
    }
}

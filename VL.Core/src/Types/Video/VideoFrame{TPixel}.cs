#nullable enable
using CommunityToolkit.HighPerformance;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public record VideoFrame<TPixel>(ReadOnlyMemory2D<TPixel> Data, string? Metadata = null, TimeSpan Timecode = default, (int N, int D) FrameRate = default)
        : VideoFrame(Metadata, Timecode, FrameRate)
        where TPixel : unmanaged, IPixel
    {
        public override int Width => Data.Width;

        public override int Height => Data.Height;

        public override PixelFormat PixelFormat => default(TPixel).PixelFormat;

        public override int PixelSizeInBytes => Unsafe.SizeOf<TPixel>();

        /// <inheritdoc />
        public override bool TryGetMemory(out ReadOnlyMemory<byte> memory)
        {
            if (Data.TryGetMemory(out var typedMemory))
            {
                memory = typedMemory.AsBytes();
                return true;
            }
            else
            {
                memory = default;
                return false;
            }
        }
    }
}
#nullable restore
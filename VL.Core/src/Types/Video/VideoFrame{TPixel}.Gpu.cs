#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public record GpuVideoFrame<TPixel>(VideoTexture Texture, string? Metadata = null, TimeSpan Timecode = default, (int N, int D) FrameRate = default)
        : VideoFrame(Metadata, Timecode, FrameRate)
        where TPixel : unmanaged, IPixel
    {
        public override int Width => Texture.Width;

        public override int Height => Texture.Height;

        public override PixelFormat PixelFormat => default(TPixel).PixelFormat;

        public override int PixelSizeInBytes => Unsafe.SizeOf<TPixel>();

        public override bool TryGetMemory(out ReadOnlyMemory<byte> memory)
        {
            memory = default;
            return false;
        }

        public override bool TryGetTexture([NotNullWhen(true)] out VideoTexture? texture)
        {
            texture = Texture;
            return true;
        }
    }
}
#nullable restore
#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public abstract record VideoFrame(string? Metadata = null, TimeSpan Timecode = default, (int N, int D) FrameRate = default)
    {
        public abstract int Width { get; }

        public abstract int Height { get; }

        public abstract PixelFormat PixelFormat { get; }

        public abstract int PixelSizeInBytes { get; }

        public int RowLengthInBytes => PixelSizeInBytes * Width;

        public int LengthInBytes => RowLengthInBytes * Height;

        public bool IsTextureBacked => TryGetTexture(out var texture) && texture != null;

        /// <summary>
        /// Tries to get a <see cref="ReadOnlyMemory{T}"/> instance, if the underlying buffer is contiguous.
        /// </summary>
        /// <param name="memory">The resulting <see cref="ReadOnlyMemory{T}"/>, in case of success.</param>
        /// <returns>Whether or not <paramref name="memory"/> was correctly assigned.</returns>
        public abstract bool TryGetMemory(out ReadOnlyMemory<byte> memory);

        public virtual bool TryGetTexture([NotNullWhen(true)] out VideoTexture? texture)
        {
            texture = null;
            return false;
        }

        public virtual Task CopyToAsync(Memory<byte> dst)
        {
            if (!TryGetMemory(out var memory))
                throw new NotImplementedException();

            memory.CopyTo(dst);

            return Task.CompletedTask;
        }
    }
}
#nullable restore
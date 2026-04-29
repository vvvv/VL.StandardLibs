#nullable enable
using System;
using System.Threading.Tasks;
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public abstract record VideoFrame(string? Metadata = null, TimeSpan Timecode = default, (int N, int D) FrameRate = default)
    {
        /// <summary>
        /// Gets the texture width in texels (GPU storage units).
        /// For most formats, this equals the logical pixel width.
        /// For packed formats like UYVY, this is half the logical width (each texel stores 2 pixels).
        /// </summary>
        public abstract int Width { get; }

        /// <summary>
        /// Gets the texture height in texels (GPU storage units).
        /// For all currently supported formats, this equals the logical pixel height.
        /// </summary>
        public abstract int Height { get; }

        public abstract PixelFormat PixelFormat { get; }

        public abstract int PixelSizeInBytes { get; }

        /// <summary>
        /// Gets the logical width in pixels (what the image represents).
        /// For packed formats like UYVY, this is Width * 2.
        /// Use this for aspect ratio calculations and display purposes.
        /// </summary>
        public int LogicalWidth => PixelFormat switch
        {
            PixelFormat.UYVY => Width * 2,
            PixelFormat.YUY2 => Width * 2,
            _ => Width
        };

        /// <summary>
        /// Gets the logical height in pixels (what the image represents).
        /// Currently same as Height for all formats.
        /// Use this for aspect ratio calculations and display purposes.
        /// </summary>
        public int LogicalHeight => Height;

        public int RowLengthInBytes => PixelSizeInBytes * Width;

        public int LengthInBytes => RowLengthInBytes * Height;

        public bool IsTextureBacked => TryGetTexture(out var texture);

        /// <summary>
        /// Gets the number of planes for this frame format.
        /// Single-plane formats (UYVY, BGRA, etc.) return 1.
        /// Multi-plane formats (P216, YV12, NV12, etc.) return 2 or 3.
        /// </summary>
        public virtual int PlaneCount => 1;

        /// <summary>
        /// Tries to get a <see cref="ReadOnlyMemory{T}"/> instance for the specified plane.
        /// </summary>
        /// <param name="planeIndex">The zero-based plane index (0 for single-plane formats).</param>
        /// <param name="memory">The resulting <see cref="ReadOnlyMemory{T}"/>, in case of success.</param>
        /// <returns>Whether or not <paramref name="memory"/> was correctly assigned.</returns>
        public virtual bool TryGetPlane(int planeIndex, out ReadOnlyMemory<byte> memory)
        {
            if (PlaneCount == 1)
                return TryGetMemory(out memory);

            memory = default;
            return false;
        }

        /// <summary>
        /// Tries to get a <see cref="ReadOnlyMemory{T}"/> instance, if the underlying buffer is contiguous.
        /// </summary>
        /// <param name="memory">The resulting <see cref="ReadOnlyMemory{T}"/>, in case of success.</param>
        /// <returns>Whether or not <paramref name="memory"/> was correctly assigned.</returns>
        public abstract bool TryGetMemory(out ReadOnlyMemory<byte> memory);

        public virtual bool TryGetTexture(out VideoTexture texture)
        {
            texture = default;
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
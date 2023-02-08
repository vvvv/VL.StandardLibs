using System;

namespace VL.Lib.Basics.Imaging
{
    /// <summary>
    /// Gives read-only access to images.
    /// </summary>
    public interface IImage
    {
        /// <summary>
        /// A structure containing size and format information of the image.
        /// </summary>
        ImageInfo Info { get; }

        /// <summary>
        /// Gives access to image's data. Must be disposed after being used.
        /// </summary>
        IImageData GetData();

        /// <summary>
        /// A volatile image is only valid in the current call stack.
        /// </summary>
        bool IsVolatile { get; }
    }

    /// <summary>
    /// Used for reading images.
    /// </summary>
    public interface IImageData : IDisposable
    {
        /// <summary>
        /// Gets the pixel data.
        /// </summary>
        ReadOnlyMemory<byte> Bytes { get; }

        /// <summary>
        /// The scan size (one row of pixels including padding) in bytes.
        /// </summary>
        int ScanSize { get; }
    }
}

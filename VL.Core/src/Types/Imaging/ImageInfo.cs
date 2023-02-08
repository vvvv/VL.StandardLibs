using System;

namespace VL.Lib.Basics.Imaging
{
    /// <summary>
    /// A structure containing size information of an image.
    /// </summary>
    public readonly struct ImageInfo : IEquatable<ImageInfo>
    {
        /// <summary>
        /// The width of the image in pixel.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// The height of the image in pixel.
        /// </summary>
        public readonly int Height;

        /// <summary>
        /// The pixel format of the image.
        /// </summary>
        public readonly PixelFormat Format;

        /// <summary>
        /// Whether or not the color channels are premultiplied with the alpha value.
        /// </summary>
        public readonly bool IsPremultipliedAlpha;

        /// <summary>
        /// The original pixel format as defined by the library the image was loaded from.
        /// Can be used in case the <see cref="Format"/> property is set to <see cref="PixelFormat.Unknown"/>.
        /// </summary>
        public readonly string OriginalFormat;

        /// <summary>
        /// Creates a new instance of the image info structure.
        /// </summary>
        /// <param name="width">The width of the image in pixel.</param>
        /// <param name="height">The height of the image in pixel.</param>
        /// <param name="format">The pixel format of the image.</param>
        /// <param name="originalFormat">The pixel format like it was provided by the original library.</param>
        public ImageInfo(int width, int height, PixelFormat format, string originalFormat = null)
            : this(width, height, format, isPremultipliedAlpha: false, originalFormat)
        {
        }

        /// <summary>
        /// Creates a new instance of the image info structure.
        /// </summary>
        /// <param name="width">The width of the image in pixel.</param>
        /// <param name="height">The height of the image in pixel.</param>
        /// <param name="format">The pixel format of the image.</param>
        /// <param name="isPremultipliedAlpha">Are the color channels pre-multiplied with the alpha value.</param>
        /// <param name="originalFormat">The pixel format like it was provided by the original library.</param>
        public ImageInfo(int width, int height, PixelFormat format, bool isPremultipliedAlpha, string originalFormat = null)
            : this(width, height, format, isPremultipliedAlpha, width * GetPixelSize(format), originalFormat)
        {
        }

        /// <summary>
        /// Creates a new instance of the image info structure.
        /// </summary>
        /// <param name="width">The width of the image in pixel.</param>
        /// <param name="height">The height of the image in pixel.</param>
        /// <param name="format">The pixel format of the image.</param>
        /// <param name="isPremultipliedAlpha">Are the color channels pre-multiplied with the alpha value.</param>
        /// <param name="scanSize">The size of one scan line (row of pixels) in bytes.</param>
        /// <param name="originalFormat">The pixel format like it was provided by the original library.</param>
        public ImageInfo(int width, int height, PixelFormat format, bool isPremultipliedAlpha, int scanSize, string originalFormat = null)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
            Format = format;
            IsPremultipliedAlpha = isPremultipliedAlpha;
            ScanSize = scanSize;
            OriginalFormat = originalFormat ?? format.ToString();
        }

        /// <summary>
        /// Splits the image info up into its components.
        /// </summary>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="format">The pixel format of the image.</param>
        /// <param name="originalFormat">The original format string (if any) of the image.</param>
        public void Split(out int width, out int height, out PixelFormat format, out string originalFormat)
        {
            width = Width;
            height = Height;
            format = Format;
            originalFormat = OriginalFormat;
        }

        /// <summary>
        /// The size of a pixel in bytes.
        /// </summary>
        public int PixelSize => GetPixelSize(Format);

        /// <summary>
        /// The size of the image in bytes.
        /// </summary>
        public int ImageSize => Height * ScanSize;

        /// <summary>
        /// The size of one scan line (row of pixels) in bytes.
        /// </summary>
        public int ScanSize { get; }

        public bool Equals(ImageInfo other) => this == other;
        public override bool Equals(object obj) => obj is ImageInfo ? Equals((ImageInfo)obj) : false;
        public override int GetHashCode() => Width ^ Height ^ Format.GetHashCode();

        public static bool operator ==(ImageInfo a, ImageInfo b) =>
            a.Width == b.Width &&
            a.Height == b.Height &&
            a.Format == b.Format &&
            a.IsPremultipliedAlpha == b.IsPremultipliedAlpha &&
            a.ScanSize == b.ScanSize &&
            a.OriginalFormat == b.OriginalFormat;

        public static bool operator !=(ImageInfo a, ImageInfo b) => !(a == b);

        private static int GetPixelSize(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Unknown:
                    return 0;
                case PixelFormat.R8:
                    return 1;
                case PixelFormat.R16:
                    return 2;
                case PixelFormat.R8G8B8:
                case PixelFormat.B8G8R8:
                    return 3;
                case PixelFormat.B8G8R8A8:
                case PixelFormat.B8G8R8X8:
                case PixelFormat.R8G8B8A8:
                case PixelFormat.R8G8B8X8:
                case PixelFormat.R32F:
                    return 4;
                case PixelFormat.R32G32F:
                case PixelFormat.R16G16B16A16F:
                    return 8;
                case PixelFormat.R32G32B32A32F:
                    return 16;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

using SkiaSharp;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VL.Lib.Basics.Imaging;
using VL.Lib.Basics.Resources;

namespace VL.Skia
{
    public static class Imaging
    {
        // We disable refcounting on the default image. Prevents it from getting disposed.
        public static readonly SKImage DefaultImage = FromImage(ImageExtensions.Default, false).NotRefCounted();

        class Image : IImage
        {
            unsafe class Data : MemoryManager<byte>, IImageData
            {
                readonly SKPixmap FPixmap;

                public Data(SKPixmap pixmap)
                {
                    FPixmap = pixmap;
                }

                public int ScanSize => FPixmap.RowBytes;

                public ReadOnlyMemory<byte> Bytes => Memory;

                public override unsafe Span<byte> GetSpan()
                {
                    return new Span<byte>(FPixmap.GetPixels().ToPointer(), FPixmap.BytesSize);
                }

                public override MemoryHandle Pin(int elementIndex = 0)
                {
                    // Already pinned
                    return new MemoryHandle(FPixmap.GetPixels().ToPointer(), pinnable: this);
                }

                public override void Unpin()
                {
                }

                protected override void Dispose(bool disposing)
                {
                    FPixmap.Dispose();
                }
            }

            internal SKImage FImage;

            public static Image New(SKImage image)
            {
                if (image is null)
                    throw new ArgumentNullException(nameof(image));
                if (image.Handle == IntPtr.Zero)
                    throw new ObjectDisposedException(nameof(image));

                var rasterImage = image.ToRasterImage(ensurePixelData: true);
                if (rasterImage is null)
                    throw new InvalidOperationException("Can't read pixels");

                return new Image(rasterImage);
            }

            private Image(SKImage image)
            {
                FImage = image ?? throw new ArgumentNullException(nameof(image));
                Info = image.ToImageInfo();
            }

            public ImageInfo Info { get; }

            public bool IsVolatile => false;

            public IImageData GetData()
            {
                // Don't cache the image data. Contract is to hand out a fresh one for each consumer.
                return new Data(FImage.PeekPixels());
            }
        }

        /// <summary>
        /// Tries to convert an IImage to an SKImage
        /// </summary>
        /// <param name="image"></param>
        /// <param name="discardHigherBits">Skia cannot handle 16bit images. Therefore in case the input is of type R16 this decides whether to discard the higher or lower bits in the conversion to 8bit</param>
        /// <param name="skImage">The converted image or null if the conversion was not possible.</param>
        /// <returns></returns>
        public static bool TryFromImage(this IImage image, bool discardHigherBits, out SKImage skImage)
        {
            if (image != null && IsSupported(image.Info.Format))
            {
                skImage = FromImage(image, discardHigherBits);
                return true;
            }
            else
            {
                skImage = default;
                return false;
            }
        }


        /// <summary>
        /// Convertes an IImage to an SKImage
        /// </summary>
        /// <param name="image"></param>
        /// <param name="discardHigherBits">Skia cannot handle 16bit images. Therefore in case the input is of type R16 this decides whether to discard the higher or lower bits in the conversion to 8bit</param>
        /// <returns></returns>
        public static unsafe SKImage FromImage(this IImage image, bool discardHigherBits)
        {
            if (image is null)
                return null;

            var info = image.Info;
            var format = info.Format;
            if (!IsSupported(format))
                throw new UnsupportedPixelFormatException(format);

            if (info.Format == PixelFormat.R16)
                return ConvertFromR16ToGray8(image, info, discardHigherBits);
            else if (info.Format == PixelFormat.R8G8B8 || info.Format == PixelFormat.B8G8R8)
                return ConvertFrom24To32(image, info);
            else if (info.Format == PixelFormat.B8G8R8X8)
                return ConvertFromBGRXToBGRA(image, info);

            var skInfo = info.ToSkImageInfo();
            if (image.IsVolatile)
            {
                using (var imageData = image.GetData())
                using (var handle = imageData.Bytes.Pin())
                {
                    return SKImage.FromPixelCopy(skInfo, new IntPtr(handle.Pointer), imageData.ScanSize);
                }
            }
            else
            {
                var imageData = image.GetData();
                var handle = imageData.Bytes.Pin();
                var pixmap = new SKPixmap(skInfo, new IntPtr(handle.Pointer), imageData.ScanSize);
                return SKImage.FromPixels(pixmap, (a, b) =>
                {
                    handle.Dispose();
                    imageData.Dispose();
                });
            }
        }

        public static unsafe SKImage AsImage<T>(this ReadOnlyMemory<T> memory, SKImageInfo info)
            where T : struct
        {
            var handle = memory.Pin();
            var data = SKData.Create(new IntPtr(handle.Pointer), Unsafe.SizeOf<T>() * memory.Length, (a, c) => handle.Dispose());
            return SKImage.FromPixels(info, data);
        }

        public static SKImage Clone(this SKImage originalImage)
        {
            if (originalImage is null)
                return null;

            using var pixmap = originalImage.PeekPixels();
            if (pixmap != null)
                return SKImage.FromPixelCopy(pixmap);
            return originalImage.ToRasterImage(ensurePixelData: true);
        }

        public static IImage ToImage(this SKImage image)
        {
            if (image is null)
                return null;

            return Image.New(image);
        }

        static bool IsSupported(PixelFormat format)
        {
            if (format.ToSkColorType() != SKColorType.Unknown)
                return true;

            // Check if we have a manual conversion
            switch (format)
            {
                case PixelFormat.R16:
                case PixelFormat.R8G8B8:
                case PixelFormat.B8G8R8:
                case PixelFormat.B8G8R8X8:
                    return true;
            }

            return false;
        }
        static unsafe SKImage ConvertFromR16ToGray8(this IImage image, ImageInfo info, bool discardHigherBits)
        {
            using (var imageData = image.GetData())
            {
                var pixelCount = info.Width * info.Height;
                var pixels = SKData.Create(pixelCount);
                fixed (byte* pSrcByte = imageData.Bytes.Span)
                {
                    var pSrc = (ushort*)pSrcByte;
                    var pDst = (byte*)pixels.Data.ToPointer();
                    if (discardHigherBits)
                    {
                        for (int i = 0; i < pixelCount; i++)
                        {
                            *pDst = (byte)(*pSrc);
                            pSrc++;
                            pDst++;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < pixelCount; i++)
                        {
                            *pDst = (byte)(*pSrc >> 8);
                            pSrc++;
                            pDst++;
                        }
                    }
                }
                return SKImage.FromPixels(
                    info: new SKImageInfo(info.Width, info.Height, SKColorType.Gray8),
                    data: pixels,
                    rowBytes: info.Width);
            }
        }

        static unsafe SKImage ConvertFromBGRXToBGRA(this IImage image, ImageInfo info)
        {
            using (var imageData = image.GetData())
            {
                var pixelCount = info.Width * info.Height;
                var pixels = SKData.Create(pixelCount * 4);
                fixed (byte* pSrc = imageData.Bytes.Span)
                {
                    var pDst = (byte*)pixels.Data.ToPointer();
                    ConvertFromBGRXToBGRA(pixelCount, pSrc, pDst);
                }
                return SKImage.FromPixels(
                    info: new SKImageInfo(info.Width, info.Height, SKColorType.Bgra8888),
                    data: pixels,
                    rowBytes: info.Width * 4);       
            }
        }

        static unsafe void ConvertFromBGRXToBGRA(int pixelCount, byte* rgbP, byte* rgbaP)
        {
            if ((pixelCount & 3) != 0)
                ConvertFromBGRXToBGRASlow(pixelCount, rgbP, (uint*)rgbaP);
            else
                ConvertFromBGRXToBGRAFast(pixelCount, rgbP, (uint*)rgbaP);
        }

        static unsafe void ConvertFromBGRXToBGRASlow(long pixelCount, byte* rgbP, uint* rgbaP)
        {
            for (long i = 0; i < pixelCount; i++)
            {
                *(rgbaP++) = *(uint*)(rgbP += 4L) | 0xff000000;
            }
        }

        static unsafe void ConvertFromBGRXToBGRAFast(long pixelCount, byte* rgbP, uint* rgbaP)
        {
            Parallel.For(0, pixelCount / 4, i =>
            {
                var d = rgbaP + i * 4L;
                var s = rgbP + i * 16L;
                d[0] = *(uint*)(s) | 0xff000000;
                d[1] = *(uint*)(s + 4L) | 0xff000000;
                d[2] = *(uint*)(s + 8L) | 0xff000000;
                d[3] = *(uint*)(s + 12L) | 0xff000000;
            });
        }

        static unsafe SKImage ConvertFrom24To32(this IImage image, ImageInfo info)
        {
            using (var imageData = image.GetData())
            {
                var pixelCount = info.Width * info.Height;
                var pixels = SKData.Create(pixelCount*4);
                fixed (byte* pSrc = imageData.Bytes.Span)
                {
                    var pDst = (byte*)pixels.Data.ToPointer();
                    ConvertFrom24To32(pixelCount, pSrc, pDst);
                }
                var colorType = info.Format == PixelFormat.R8G8B8 ? SKColorType.Rgba8888 : SKColorType.Bgra8888;
                return SKImage.FromPixels(
                    info: new SKImageInfo(info.Width, info.Height, colorType),
                    data: pixels,
                    rowBytes: info.Width*4);
            }
        }

        static unsafe void ConvertFrom24To32(int pixelCount, byte* rgbP, byte* rgbaP)
        {
            if ((pixelCount & 3) != 0)
                ConvertFrom24To32Slow(pixelCount, rgbP, (uint*)rgbaP);
            else
                ConvertFrom24To32Fast(pixelCount, rgbP, (uint*)rgbaP);
        }

        static unsafe void ConvertFrom24To32Slow(long pixelCount, byte* rgbP, uint* rgbaP)
        {
            for (long i = 0; i < pixelCount; i++)
            {
                *(rgbaP++) = *(uint*)(rgbP += 3L) | 0xff000000;
            }
        }

        static unsafe void ConvertFrom24To32Fast(long pixelCount, byte* rgbP, uint* rgbaP)
        {
            Parallel.For(0, pixelCount / 4, i =>
            {
                var d = rgbaP + i * 4L;
                var s = rgbP + i * 12L;
                d[0] = *(uint*)(s) | 0xff000000;
                d[1] = *(uint*)(s + 3L) | 0xff000000;
                d[2] = *(uint*)(s + 6L) | 0xff000000;
                d[3] = *(uint*)(s + 9L) | 0xff000000;
            });
        }

        static SKImageInfo ToSkImageInfo(this ImageInfo info)
        {
            var colorType = info.Format.ToSkColorType();
            return new SKImageInfo(info.Width, info.Height, colorType, info.IsPremultipliedAlpha ? SKAlphaType.Premul : SKAlphaType.Unpremul);
        }

        static ImageInfo ToImageInfo(this SKImageInfo info)
        {
            var format = info.ColorType.ToPixelFormat();
            return new ImageInfo(info.Width, info.Height, format, info.AlphaType == SKAlphaType.Premul, info.ColorType.ToString());
        }

        static ImageInfo ToImageInfo(this SKImage image)
        {
            var format = image.ColorType.ToPixelFormat();
            return new ImageInfo(image.Width, image.Height, format, image.AlphaType == SKAlphaType.Premul, image.ColorType.ToString());
        }

        internal static SKColorType ToSkColorType(this PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Unknown:
                    return SKColorType.Unknown;
                case PixelFormat.R8:
                    return SKColorType.Gray8;
                case PixelFormat.R32F:
                    break;
                case PixelFormat.R8G8B8:
                    break;
                case PixelFormat.R8G8B8X8:
                    return SKColorType.Rgb888x;
                case PixelFormat.R8G8B8A8:
                    return SKColorType.Rgba8888;
                case PixelFormat.B8G8R8X8:
                    break;
                case PixelFormat.B8G8R8A8:
                    return SKColorType.Bgra8888;
                case PixelFormat.R16G16B16A16F:
                    return SKColorType.RgbaF16;
                case PixelFormat.R32G32B32A32F:
                    return SKColorType.RgbaF32;
                default:
                    break;
            }
            return SKColorType.Unknown;
        }

        static PixelFormat ToPixelFormat(this SKColorType colorType)
        {
            switch (colorType)
            {
                case SKColorType.Unknown:
                    return PixelFormat.Unknown;
                case SKColorType.Alpha8:
                    break;
                case SKColorType.Rgb565:
                    break;
                case SKColorType.Argb4444:
                    break;
                case SKColorType.Rgb888x:
                    return PixelFormat.R8G8B8X8;
                case SKColorType.Rgba8888:
                    return PixelFormat.R8G8B8A8;
                case SKColorType.Bgra8888:
                    return PixelFormat.B8G8R8A8;
                case SKColorType.Gray8:
                    return PixelFormat.R8;
                case SKColorType.RgbaF16:
                    return PixelFormat.R16G16B16A16F;
                case SKColorType.RgbaF32:
                    return PixelFormat.R32G32B32A32F;
                default:
                    break;
            }
            return PixelFormat.Unknown;
        }
    }
}

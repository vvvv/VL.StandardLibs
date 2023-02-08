using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using VL.Lib.Collections;

namespace VL.Lib.Basics.Imaging
{
    public static class ImageExtensions
    {
        /// <summary>
        /// A white one by one pixel RGBA image.
        /// </summary>
        public static readonly IImage Default = new ArrayImage<byte>(((byte)255).YieldReturnMany(4*256*256).ToArray(), new ImageInfo(256, 256, PixelFormat.R8G8B8A8), false);

        #region API compatibility

        /// <summary>
        /// Makes the memory accessible as an image. No data gets copied.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="data">The pixel data.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="format">The pixel format of the image.</param>
        /// <param name="originalFormat">An optional hint at the original pixel format.</param>
        /// <param name="isVolatile">Whether or not provided memory is only valid in the current call stack.</param>
        /// <returns>An image which uses the given memory as its backing store.</returns>
        public static IImage ToImage<T>(this T[] data, int width, int height, PixelFormat format, string originalFormat = null, bool isVolatile = false)
            where T : struct
        {
            return ToImage(data, width, height, format, isPremultipliedAlpha: false, originalFormat, isVolatile);
        }

        /// <summary>
        /// Makes the memory accessible as an image. No data gets copied.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="data">The pixel data.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="format">The pixel format of the image.</param>
        /// <param name="originalFormat">An optional hint at the original pixel format.</param>
        /// <param name="isVolatile">Whether or not provided memory is only valid in the current call stack.</param>
        /// <returns>An image which uses the given memory as its backing store.</returns>
        public static IImage ToImage<T>(this Spread<T> data, int width, int height, PixelFormat format, string originalFormat = null, bool isVolatile = false)
            where T : struct
        {
            return ToImage(data, width, height, format, isPremultipliedAlpha: false, originalFormat, isVolatile);
        }

        /// <summary>
        /// Makes the memory accessible as an image. No data gets copied.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="data">The pixel data.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="format">The pixel format of the image.</param>
        /// <param name="originalFormat">An optional hint at the original pixel format.</param>
        /// <param name="isVolatile">Whether or not provided memory is only valid in the current call stack.</param>
        /// <returns>An image which uses the given memory as its backing store.</returns>
        public static IImage ToImage<T>(this IReadOnlyList<T> data, int width, int height, PixelFormat format, string originalFormat = null, bool isVolatile = false)
            where T : struct
        {
            return ToImage(data, width, height, format, isPremultipliedAlpha: false, originalFormat, isVolatile);
        }

        /// <summary>
        /// Makes the memory accessible as an image. No data gets copied.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="data">The pixel data.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="format">The pixel format of the image.</param>
        /// <param name="originalFormat">An optional hint at the original pixel format.</param>
        /// <param name="isVolatile">Whether or not provided memory is only valid in the current call stack.</param>
        /// <returns>An image which uses the given memory as its backing store.</returns>
        public static IImage ToImage<T>(this ReadOnlyMemory<T> data, int width, int height, PixelFormat format, string originalFormat = null, bool isVolatile = false)
            where T : struct
        {
            return ToImage(data, width, height, format, isPremultipliedAlpha: false, originalFormat, isVolatile);
        }

        #endregion

        /// <summary>
        /// Makes the array accessible as an image. No data gets copied.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="data">The pixel data.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="format">The pixel format of the image.</param>
        /// <param name="isPremultipliedAlpha">Are the color channels pre-multiplied with the alpha value.</param>
        /// <param name="originalFormat">An optional hint at the original pixel format.</param>
        /// <param name="isVolatile">Whether or not provided array is only valid in the current call stack.</param>
        /// <returns>An image which uses the array as its backing store.</returns>
        public static IImage ToImage<T>(this T[] data, int width, int height, PixelFormat format, bool isPremultipliedAlpha, string originalFormat = null, bool isVolatile = false)
            where T : struct
            => ToImage((ReadOnlyMemory<T>)data.AsMemory(), width, height, format, isPremultipliedAlpha, originalFormat, isVolatile);

        /// <summary>
        /// Makes the spread accessible as an image. No data gets copied.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="data">The pixel data.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="format">The pixel format of the image.</param>
        /// <param name="isPremultipliedAlpha">Are the color channels pre-multiplied with the alpha value.</param>
        /// <param name="originalFormat">An optional hint at the original pixel format.</param>
        /// <returns>An image which uses the spread as its backing store.</returns>
        public static IImage ToImage<T>(this Spread<T> data, int width, int height, PixelFormat format, bool isPremultipliedAlpha, string originalFormat = null)
            where T : struct
            => ToImage(data.AsMemory(), width, height, format, isPremultipliedAlpha, originalFormat, false);

        /// <summary>
        /// Tries to fetch the backing memory of the given <see cref="IReadOnlyList{T}"/> and makes its data accessible as an image. No data gets copied. 
        /// In case the data can't be accessed as a <see cref="ReadOnlyMemory{T}"/> the default image will be returned.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="data">The pixel data.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="format">The pixel format of the image.</param>
        /// <param name="isPremultipliedAlpha">Are the color channels pre-multiplied with the alpha value.</param>
        /// <param name="originalFormat">An optional hint at the original pixel format.</param>
        /// <param name="isVolatile">Whether or not provided memory is only valid in the current call stack.</param>
        /// <returns>An image using the given data as its backing store or the default image if backing memory couldn't be fetched.</returns>
        public static IImage ToImage<T>(this IReadOnlyList<T> data, int width, int height, PixelFormat format, bool isPremultipliedAlpha, string originalFormat = null, bool isVolatile = false)
            where T : struct
        {
            if (data.TryGetMemory(out var memory))
                return ToImage(memory, width, height, format, isPremultipliedAlpha, originalFormat, isVolatile);
            else
                return Default;
        }

        /// <summary>
        /// Makes the memory accessible as an image. No data gets copied.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="data">The pixel data.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="format">The pixel format of the image.</param>
        /// <param name="isPremultipliedAlpha">Are the color channels pre-multiplied with the alpha value.</param>
        /// <param name="originalFormat">An optional hint at the original pixel format.</param>
        /// <param name="isVolatile">Whether or not provided memory is only valid in the current call stack.</param>
        /// <returns>An image which uses the given memory as its backing store.</returns>
        public static IImage ToImage<T>(this ReadOnlyMemory<T> data, int width, int height, PixelFormat format, bool isPremultipliedAlpha, string originalFormat = null, bool isVolatile = false)
            where T : struct
        {
            var info = new ImageInfo(Math.Max(width, 0), Math.Max(height, 0), format, isPremultipliedAlpha, originalFormat);
            if (format != PixelFormat.Unknown && info.ImageSize > (data.Length * Unsafe.SizeOf<T>()))
                throw new ArgumentException("The given dimensions (width * height * sizeof(format)) are greater than the length in bytes of the given data.");
            return new MemoryBasedImage<T>(data, new ImageInfo(Math.Max(width, 0), Math.Max(height, 0), format, originalFormat), isVolatile);
        }

        /// <summary>
        /// Makes the bitmap accessible as an image. No data gets copied.
        /// </summary>
        /// <param name="bitmap">The bitmap to wrap.</param>
        /// <param name="takeOwnership">Whether or not the wrapper should take ownership of the bitmap. So in case it gets disposed the bitmap will also get disposed.</param>
        /// <param name="isVolatile">Whether or not the returned image is marked as volatile. Should be true in case the bitmap gets modified later.</param>
        /// <param name="canWrite">Whether or not one can write into the bitmap.</param>
        /// <returns>The image wrapping the bitmap.</returns>
        [SupportedOSPlatform("windows")]
        public static BitmapImage ToImage(this Bitmap bitmap, bool takeOwnership, bool isVolatile = true, bool canWrite = false) 
            => new BitmapImage(bitmap, takeOwnership, isVolatile, canWrite);

        /// <summary>
        /// Makes the pointer accessible as an image. No data gets copied. The returned image is volatile and should get disposed of.
        /// Once disposed further access to <see cref="IImage.GetData"/> will return the image data from the default image.
        /// </summary>
        /// <param name="pointer">The pointer to the data of the image.</param>
        /// <param name="size">The size in bytes of the image data.</param>
        /// <param name="width">The width in pixel.</param>
        /// <param name="height">The height in pixel.</param>
        /// <param name="format">The pixel format.</param>
        /// <param name="originalFormat">An optional hint at the original pixel format.</param>
        /// <returns>A volatile image using the pointer as its backing store.</returns>
        public static IntPtrImage ToImage(this IntPtr pointer, int size, int width, int height, PixelFormat format, string originalFormat = null)
            => new IntPtrImage(pointer, size, new ImageInfo(width, height, format, originalFormat));

        /// <summary>
        /// Clones the image.
        /// </summary>
        /// <param name="image">The image to clone.</param>
        /// <returns>The cloned image.</returns>
        public static unsafe IImage Clone(this IImage image)
        {
            using (var data = image.GetData())
            {
                var array = data.Bytes.ToArray();
                return new ArrayImage<byte>(array, image.Info, false, data.ScanSize);
            }
        }

        /// <summary>
        /// Creates an empty clone of the image.
        /// </summary>
        /// <param name="image">The image to clone.</param>
        /// <returns>An image of the same size and format but with all pixels set to zero.</returns>
        public static unsafe IImage CloneEmpty(this IImage image)
        {
            using (var data = image.GetData())
            {
                var array = new byte[data.Bytes.Length];
                return new ArrayImage<byte>(array, image.Info, false, data.ScanSize);
            }
        }

        /// <summary>
        /// Creates a new image instance without copying any data. Useful to propagate changed information.
        /// </summary>
        /// <param name="image">The image to "re-sell" as a new image.</param>
        /// <returns>A new image instance reusing all the data of the input image.</returns>
        public static IImage AsNewImage(this IImage image)
        {
            return new ProxyImage(image);
        }

        /// <summary>
        /// Whether or not the image info is the same.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="other">The other image to compare the info with.</param>
        /// <returns>True if the image info is the same</returns>
        public static bool HasSameInfo(this IImage image, IImage other)
        {
            return image.Info == other?.Info;
        }

        /// <summary>
        /// Copies the source image to the destination location. If the destination is not yet assigned or if the image
        /// information doesn't fit a new image will be created and assigned to the destination.
        /// </summary>
        /// <param name="src">The image to copy from.</param>
        /// <param name="dst">The destination location to copy the image to.</param>
        public static unsafe void CopyTo(this IImage src, ref IImage dst)
        {
            // Load into local
            var d = dst;
            if (d == null || src.Info != d.Info || d == Default)
                d = src.CloneEmpty();
            using (var srcData = src.GetData())
            using (var dstData = d.GetData())
            {
                CopyTo(srcData, dstData);
            }
            // Write local back
            dst = d;
        }

        /// <summary>
        /// Copies the source image to the destination image. The source and destination images must have the same format.
        /// </summary>
        /// <param name="src">The image to copy from.</param>
        /// <param name="dst">The image to copy to.</param>
        public static unsafe void CopyTo(this IImage src, IImage dst)
        {
            if (src.Info != dst.Info)
                throw new ArgumentException($"The image info of {nameof(src)} and {nameof(dst)} must be the same.");

            using (var srcData = src.GetData())
            using (var dstData = dst.GetData())
            {
                CopyTo(srcData, dstData);
            }
        }

        /// <summary>
        /// Makes the image accessible as a stream.
        /// </summary>
        /// <param name="image">The image to wrap.</param>
        /// <returns>A stream which will read from the image.</returns>
        public static Stream ToStream(this IImage image)
        {
            var data = image.GetData();
            return new ImageStream(data);
        }

        /// <summary>
        /// Copies the pixels of the image to the destination.
        /// </summary>
        /// <param name="srcData">The source image data.</param>
        /// <param name="dstData">The destination image data.</param>
        public static unsafe void CopyTo(IImageData srcData, IImageData dstData)
        {
            var srcBytes = srcData.Bytes.Span;
            var dstBytes = MemoryMarshal.AsMemory(dstData.Bytes).Span;
            var srcScan = srcData.ScanSize;
            var dstScan = dstData.ScanSize;
            if (srcScan == dstScan)
            {
                srcBytes.Slice(0, dstBytes.Length).CopyTo(dstBytes);
            }
            else
            {
                // Copy row by row
                var rowCount = srcBytes.Length / srcScan;
                // Seen when copying from DX11 to OpenCV, the srcScan is larger than the dstScan.
                var scan = Math.Min(srcScan, dstScan);
                for (int i = 0; i < rowCount; i++)
                    srcBytes.Slice(i * srcScan, scan).CopyTo(dstBytes.Slice(i * dstScan, scan));
            }
        }

        static ConditionalWeakTable<Bitmap, FinalizableHandle> WeakBitmapImageDataTable;

        /// <summary>
        /// Makes the <see cref="IImage"/> accessible as a <see cref="Bitmap"/> by either copying the pixels or wrapping them.
        /// </summary>
        /// <param name="image">The image to either copy the pixels from or wrap in a <see cref="Bitmap"/>.</param>
        /// <param name="copy">Whether or not to copy the pixels.</param>
        /// <returns>The <see cref="Bitmap"/> containing either the copied pixels or pointing to the upstream image data.</returns>
        [SupportedOSPlatform("windows")]
        public static unsafe Bitmap FromImage(this IImage image, bool copy)
        {
            var info = image.Info;
            var pixelFormat = info.Format.ToPixelFormat();
            if (copy)
            {
                var bitmap = new Bitmap(info.Width, info.Height, pixelFormat);
                var bitmapImage = bitmap.ToImage(takeOwnership: false, canWrite: true);
                using (var srcData = image.GetData())
                using (var dstData = bitmapImage.GetData())
                {
                    CopyTo(srcData, dstData);
                }
                return bitmap;
            }
            else
            {
                var imageData = image.GetData();
                var memoryHandle = imageData.Bytes.Pin();
                var bitmap = new Bitmap(info.Width, info.Height, imageData.ScanSize, pixelFormat, new IntPtr(memoryHandle.Pointer));
                // Tie the bitmap and the pinned memory
                var handle = new FinalizableHandle(imageData, memoryHandle);
                (WeakBitmapImageDataTable ??= new ConditionalWeakTable<Bitmap, FinalizableHandle>()).Add(bitmap, handle);
                return bitmap;
            }
        }

        [SupportedOSPlatform("windows")]
        public static PixelFormat ToPixelFormat(this System.Drawing.Imaging.PixelFormat format)
        {
            switch (format)
            {
                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                    return PixelFormat.R8G8B8;
                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                    return PixelFormat.B8G8R8X8;
                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                    return PixelFormat.B8G8R8A8;
                default:
                    return PixelFormat.Unknown;
            }
        }

        [SupportedOSPlatform("windows")]
        public static System.Drawing.Imaging.PixelFormat ToPixelFormat(this PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Unknown:
                    break;
                case PixelFormat.R8:
                    break;
                case PixelFormat.R16:
                    break;
                case PixelFormat.R32F:
                    break;
                case PixelFormat.R8G8B8:
                case PixelFormat.B8G8R8:
                    return System.Drawing.Imaging.PixelFormat.Format24bppRgb;
                case PixelFormat.R8G8B8X8:
                case PixelFormat.B8G8R8X8:
                    return System.Drawing.Imaging.PixelFormat.Format32bppRgb;
                case PixelFormat.R8G8B8A8:
                case PixelFormat.B8G8R8A8:
                    return System.Drawing.Imaging.PixelFormat.Format32bppArgb;
                case PixelFormat.R32G32B32A32F:
                    break;
                default:
                    break;
            }
            return System.Drawing.Imaging.PixelFormat.Undefined;
        }

        /// <summary>
        /// Gets the pixel size in bytes.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static int GetPixelSize(this PixelFormat format)
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
                case PixelFormat.R32F:
                case PixelFormat.R8G8B8X8:
                case PixelFormat.B8G8R8X8:
                case PixelFormat.R8G8B8A8:
                case PixelFormat.B8G8R8A8:
                    return 4;
                case PixelFormat.R32G32F:
                case PixelFormat.R16G16B16A16F:
                    return 8;
                case PixelFormat.R32G32B32A32F:
                    return 16;
            }

            return 0;
        }
    }
}

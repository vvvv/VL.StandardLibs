using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.Basics.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace VL.Lib
{
    public enum BitmapFileFormat
    {
        Bmp,
        Gif,
        Icon,
        Jpeg,
        Png,
        Tiff
    }

    [SupportedOSPlatform("windows")]
    public static class BitmapNodes
    {
        public static readonly Bitmap DefaultBitmap = ImageExtensions.Default.FromImage(copy: true);

        /// <summary>
        /// Copy the pixel data of a Bitmap into a new one. Uses the same code path as Convert and also copies the metadata PropertyItems.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="disposeInputBitmap">If set to <c>true</c> disposes input bitmap after the copy.</param>
        /// <returns>
        /// The new Bitmap.
        /// </returns>
        public static Bitmap Copy(Bitmap input, bool disposeInputBitmap = false)
        {
            var result = Convert(input, input.PixelFormat, disposeInputBitmap);

            foreach(var prop in input.PropertyItems)
            {
                result.SetPropertyItem(prop);
            }

            return result;
        }

        /// <summary>
        /// Convert a Bitmap to the specified pixel format.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="pixelFormat">The target bitmap pixel format.</param>
        /// <param name="disposeInputBitmap">If set to <c>true</c> disposes input bitmap after conversion.</param>
        /// <returns>
        /// The new, Bitmap with converted pixels.
        /// </returns>
        public static Bitmap Convert(Bitmap input, PixelFormat pixelFormat = PixelFormat.Format32bppArgb, bool disposeInputBitmap = false)
        {
            if (input != null)
            {
                if (input.PixelFormat == pixelFormat)
                {
                    return input;
                }

                var result = new Bitmap(input.Width, input.Height, pixelFormat);

                using (var graphics = System.Drawing.Graphics.FromImage(result))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.DrawImage(input, new Rectangle(0, 0, result.Width, result.Height));
                }

                result.SetResolution(input.HorizontalResolution, input.VerticalResolution);

                if (disposeInputBitmap && input != DefaultBitmap)
                    input.Dispose();

                return result;
            }

            return DefaultBitmap;
        }

        /// <summary>
        /// Resize the Bitmap to the specified width and height.
        /// </summary>
        /// <param name="input">The Bitmap to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <param name="interpolationMode">The interpolation mode.</param>
        /// <param name="disposeInputBitmap">If set to <c>true</c> disposes input bitmap after conversion.</param>
        /// <returns>
        /// The resized and converted Bitmap.
        /// </returns>
        public static Bitmap Resize(Bitmap input, int width = 32, int height = 32, InterpolationMode interpolationMode = InterpolationMode.HighQualityBicubic, bool disposeInputBitmap = false)
        {
            return ResizeAndConvert(input, width, height, interpolationMode, input.PixelFormat, disposeInputBitmap);
        }


        /// <summary>
        /// Resize and convert the Bitmap to the specified width, height and pixel format.
        /// </summary>
        /// <param name="input">The Bitmap to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <param name="interpolationMode">The interpolation mode.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <param name="disposeInputBitmap">If set to <c>true</c> disposes input bitmap after conversion.</param>
        /// <returns>
        /// The resized and converted Bitmap.
        /// </returns>
        public static Bitmap ResizeAndConvert(Bitmap input, int width = 32, int height = 32, InterpolationMode interpolationMode = InterpolationMode.HighQualityBicubic, PixelFormat pixelFormat = PixelFormat.Format32bppArgb, bool disposeInputBitmap = false)
        {
            if (input != null)
            {
                if (input.Width == width && input.Height == height && input.PixelFormat == pixelFormat)
                {
                    return input;
                }

                var destRect = new Rectangle(0, 0, width, height);
                var destBitmap = new Bitmap(width, height, pixelFormat);
                destBitmap.SetResolution(input.HorizontalResolution, input.VerticalResolution);

                using (var graphics = System.Drawing.Graphics.FromImage(destBitmap))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    //graphics.CompositingQuality = CompositingQuality.HighQuality; //not needed, only drawing one image
                    graphics.InterpolationMode = interpolationMode;
                    //graphics.SmoothingMode = SmoothingMode.HighQuality; //only needed for vector shapes
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    using (var wrapMode = new ImageAttributes())
                    {
                        wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                        graphics.DrawImage(input, destRect, 0, 0, input.Width, input.Height, GraphicsUnit.Pixel, wrapMode);
                    }
                }

                if (disposeInputBitmap && input != DefaultBitmap)
                    input.Dispose();

                return destBitmap;
            }

            return DefaultBitmap;
        }

        public static ImageFormat BitmapFileFormatToImageFormat(BitmapFileFormat input)
        {
            switch (input)
            {
                case BitmapFileFormat.Bmp:
                    return ImageFormat.Bmp;
                case BitmapFileFormat.Gif:
                    return ImageFormat.Gif;
                case BitmapFileFormat.Icon:
                    return ImageFormat.Icon;
                case BitmapFileFormat.Jpeg:
                    return ImageFormat.Jpeg;
                case BitmapFileFormat.Png:
                    return ImageFormat.Png;
                case BitmapFileFormat.Tiff:
                    return ImageFormat.Tiff;
                default:
                    return ImageFormat.Bmp;
            }
        }

        public static string BitmapFileFormatToExtension(BitmapFileFormat input)
        {
            switch (input)
            {
                case BitmapFileFormat.Bmp:
                    return ".bmp";
                case BitmapFileFormat.Gif:
                    return ".gif";
                case BitmapFileFormat.Icon:
                    return ".ico";
                case BitmapFileFormat.Jpeg:
                    return ".jpg";
                case BitmapFileFormat.Png:
                    return ".png";
                case BitmapFileFormat.Tiff:
                    return ".tif";
                default:
                    return ".bmp";
            }
        }

        /// <summary>
        /// Saves a bitmap to a file on disc with specified file format. Also takes care of the file extension.
        /// </summary>
        /// <param name="input">The input bitmap.</param>
        /// <param name="filename">The full path.</param>
        /// <param name="format">The image file format.</param>
        /// <param name="save">If set to <c>true</c> saves the file to disc.</param>
        /// <param name="disposeInputBitmap">If set to <c>true</c> disposes input bitmap after save. Useful when the node is the last in a Bitmap processing chain and no other node uses it anymore.</param>
        /// <returns> Same Bitmap instance as the input.</returns>
        public static Bitmap Save(Bitmap input, string filename, BitmapFileFormat format, bool save, bool disposeInputBitmap = false)
        {
            if (save)
            {
                var imageFormat = BitmapFileFormatToImageFormat(format);
                var extension = BitmapFileFormatToExtension(format);

                return SaveCustom(input, Path.ChangeExtension(filename, extension), imageFormat, true, disposeInputBitmap);
            }

            return input;
        }

        /// <summary>
        /// Saves a bitmap to a file on disc with specified .NET image format.
        /// </summary>
        /// <param name="input">The input bitmap.</param>
        /// <param name="filename">The full path.</param>
        /// <param name="format">The image file format.</param>
        /// <param name="save">If set to <c>true</c> saves the file to disc.</param>
        /// <param name="disposeInputBitmap">If set to <c>true</c> disposes input bitmap after save. Useful when the node is the last in a Bitmap processing chain and no other node uses it anymore.</param>
        /// <returns> Same Bitmap instance as the input.</returns>
        public static Bitmap SaveCustom(Bitmap input, string filename, ImageFormat format, bool save, bool disposeInputBitmap = false)
        {
            if (save)
            {
                input.Save(filename, format);

                if (disposeInputBitmap && input != DefaultBitmap)
                    input.Dispose(); 
            }

            return input;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using Stride.Core.Mathematics;

namespace VL.Lib.ColorMath
{
    public static class ColorConversions
    {
        #region sRGB

        /// <summary>
        /// Converts sRGB color values to RGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="srgb">
        /// Color value to convert in sRGB.
        /// </param>
        public static Color4 SRGBToRGB(Color4 srgb)
        {
            float r, g, b;

            if (srgb.R <= 0.04045f)
            {
                r = srgb.R / 12.92f;
            }
            else
            {
                r = (float)Math.Pow((srgb.R + 0.055f) / (1.0f + 0.055f), 2.4f);
            }

            if (srgb.G <= 0.04045f)
            {
                g = srgb.G / 12.92f;
            }
            else
            {
                g = (float)Math.Pow((srgb.G + 0.055f) / (1.0f + 0.055f), 2.4f);
            }

            if (srgb.B <= 0.04045f)
            {
                b = srgb.B / 12.92f;
            }
            else
            {
                b = (float)Math.Pow((srgb.B + 0.055f) / (1.0f + 0.055f), 2.4f);
            }

            return new Color4(r, g, b, srgb.A);
        }

        /// <summary>
        /// Converts RGB color values to sRGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        public static Color4 RGBToSRGB(Color4 rgb)
        {
            float r, g, b;

            if (rgb.R <= 0.0031308)
            {
                r = 12.92f * rgb.R;
            }
            else
            {
                r = (1.0f + 0.055f) * (float)Math.Pow(rgb.R, 1.0f / 2.4f) - 0.055f;
            }

            if (rgb.G <= 0.0031308)
            {
                g = 12.92f * rgb.G;
            }
            else
            {
                g = (1.0f + 0.055f) * (float)Math.Pow(rgb.G, 1.0f / 2.4f) - 0.055f;
            }

            if (rgb.B <= 0.0031308)
            {
                b = 12.92f * rgb.B;
            }
            else
            {
                b = (1.0f + 0.055f) * (float)Math.Pow(rgb.B, 1.0f / 2.4f) - 0.055f;
            }

            return new Color4(r, g, b, rgb.A);
        }

        #endregion

        #region HSL
        public static Color4 FromHSL(float hue = 0.33333333f, float saturation = 1, float lightness = 0.5f, float alpha = 1)
        {
            return FromHSL(new Vector4(hue, saturation, lightness, alpha));
        }

        /// <summary>
        /// Converts HSL color values to RGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="hsl">
        /// Color value to convert in hue, saturation, lightness (HSL).
        /// The X element is Hue (H), the Y element is Saturation (S), the Z element is Lightness (L), and the W element is Alpha (which is copied to the output's Alpha value).
        /// Each has a range of 0.0 to 1.0.
        /// </param>
        public static Color4 FromHSL(Vector4 hsl)
        {
            var hue = hsl.X % 1.0f;
            if (hue < 0)
            {
                hue += 1.0f;
            }

            hue *= 360.0f;

            var saturation = hsl.Y;
            var lightness = hsl.Z;

            var C = (1.0f - Math.Abs(2.0f * lightness - 1.0f)) * saturation;

            var h = hue / 60.0f;
            var X = C * (1.0f - Math.Abs(h % 2.0f - 1.0f));

            float r, g, b;
            if (0.0f <= h && h < 1.0f)
            {
                r = C;
                g = X;
                b = 0.0f;
            }
            else if (1.0f <= h && h < 2.0f)
            {
                r = X;
                g = C;
                b = 0.0f;
            }
            else if (2.0f <= h && h < 3.0f)
            {
                r = 0.0f;
                g = C;
                b = X;
            }
            else if (3.0f <= h && h < 4.0f)
            {
                r = 0.0f;
                g = X;
                b = C;
            }
            else if (4.0f <= h && h < 5.0f)
            {
                r = X;
                g = 0.0f;
                b = C;
            }
            else if (5.0f <= h && h < 6.0f)
            {
                r = C;
                g = 0.0f;
                b = X;
            }
            else
            {
                r = 0.0f;
                g = 0.0f;
                b = 0.0f;
            }

            var m = lightness - (C / 2.0f);
            return new Color4(r + m, g + m, b + m, hsl.W);
        }

        /// <summary>
        /// Converts RGB color values to HSL color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// The X element is Hue (H), the Y element is Saturation (S), the Z element is Lightness (L), and the W element is Alpha (a copy of the input's Alpha value).
        /// Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        public static Vector4 ToHSL(Color4 rgb)
        {
            var M = Math.Max(rgb.R, Math.Max(rgb.G, rgb.B));
            var m = Math.Min(rgb.R, Math.Min(rgb.G, rgb.B));
            var C = M - m;

            float hue = 0.0f;
            if (M == rgb.R)
            {
                hue = ((rgb.G - rgb.B) / C);
            }
            else if (M == rgb.G)
            {
                hue = ((rgb.B - rgb.R) / C) + 2.0f;
            }
            else if (M == rgb.B)
            {
                hue = ((rgb.R - rgb.G) / C) + 4.0f;
            }

            hue /= 6.0f;
            if (hue < 0.0f)
                hue += 1.0f;

            var lightness = (M + m) / 2.0f;

            var saturation = 0.0f;
            if (0.0f != lightness && lightness != 1.0f)
            {
                saturation = C / (1.0f - Math.Abs(2.0f * lightness - 1.0f));
            }

            return new Vector4(hue, saturation, lightness, rgb.A);
        }

        public static void ToHSL(Color4 rgb, out float hue, out float saturation, out float lightness, out float alpha)
        {
            var hsl = ToHSL(rgb);
            hue = hsl.X;
            saturation = hsl.Y;
            lightness = hsl.Z;
            alpha = hsl.W;
        }

        #endregion

        #region HSV
        public static Color4 FromHSV(float hue = 0.33333333f, float saturation = 1, float value = 1, float alpha = 1)
        {
            return FromHSV(new Vector4(hue, saturation, value, alpha));
        }

        /// <summary>
        /// Converts HSV color values to RGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="hsv">
        /// Color value to convert in hue, saturation, value (HSV).
        /// The X element is Hue (H), the Y element is Saturation (S), the Z element is Value (V), and the W element is Alpha (which is copied to the output's Alpha value).
        /// Each has a range of 0.0 to 1.0.
        /// </param>
        public static Color4 FromHSV(Vector4 hsv)
        {
            var hue = hsv.X % 1.0f;
            if (hue < 0)
            {
                hue += 1.0f;
            }

            hue *= 360.0f;

            var saturation = hsv.Y;
            var value = hsv.Z;

            var C = value * saturation;

            var h = hue / 60.0f;
            var X = C * (1.0f - Math.Abs(h % 2.0f - 1.0f));

            float r, g, b;
            if (0.0f <= h && h < 1.0f)
            {
                r = C;
                g = X;
                b = 0.0f;
            }
            else if (1.0f <= h && h < 2.0f)
            {
                r = X;
                g = C;
                b = 0.0f;
            }
            else if (2.0f <= h && h < 3.0f)
            {
                r = 0.0f;
                g = C;
                b = X;
            }
            else if (3.0f <= h && h < 4.0f)
            {
                r = 0.0f;
                g = X;
                b = C;
            }
            else if (4.0f <= h && h < 5.0f)
            {
                r = X;
                g = 0.0f;
                b = C;
            }
            else if (5.0f <= h && h < 6.0f)
            {
                r = C;
                g = 0.0f;
                b = X;
            }
            else
            {
                r = 0.0f;
                g = 0.0f;
                b = 0.0f;
            }

            var m = value - C;
            return new Color4(r + m, g + m, b + m, hsv.W);
        }

        /// <summary>
        /// Converts RGB color values to HSV color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// The X element is Hue (H), the Y element is Saturation (S), the Z element is Value (V), and the W element is Alpha (a copy of the input's Alpha value).
        /// Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        public static Vector4 ToHSV(Color4 rgb)
        {
            var M = Math.Max(rgb.R, Math.Max(rgb.G, rgb.B));
            var m = Math.Min(rgb.R, Math.Min(rgb.G, rgb.B));
            var C = M - m;

            float hue = 0.0f;
            if (C > 0f)
            {
                if (M == rgb.R)
                {
                    hue = ((rgb.G - rgb.B) / C) % 6.0f;
                }
                else if (M == rgb.G)
                {
                    hue = ((rgb.B - rgb.R) / C) + 2.0f;
                }
                else if (M == rgb.B)
                {
                    hue = ((rgb.R - rgb.G) / C) + 4.0f;
                }
            }

            hue /= 6.0f;
            if (hue < 0.0f)
                hue += 1.0f;

            var saturation = 0.0f;
            if (0.0f != M)
            {
                saturation = C / M;
            }

            return new Vector4(hue, saturation, M, rgb.A);
        }

        public static void ToHSV(Color4 rgb, out float hue, out float saturation, out float value, out float alpha)
        {
            var hsv = ToHSV(rgb);
            hue = hsv.X;
            saturation = hsv.Y;
            value = hsv.Z;
            alpha = hsv.W;
        }

        #endregion

        #region XYZ

        /// <summary>
        /// Converts XYZ color values to RGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="xyz">
        /// Color value to convert with the trisimulus values of X, Y, and Z in the corresponding element, and the W element with Alpha (which is copied to the output's Alpha value).
        /// Each has a range of 0.0 to 1.0.
        /// </param>
        /// <remarks>Uses the CIE XYZ colorspace.</remarks>
        public static Color4 FromXYZ(Vector4 xyz)
        {
            var r = 0.41847f * xyz.X + -0.15866f * xyz.Y + -0.082835f * xyz.Z;
            var g = -0.091169f * xyz.X + 0.25243f * xyz.Y + 0.015708f * xyz.Z;
            var b = 0.00092090f * xyz.X + -0.0025498f * xyz.Y + 0.17860f * xyz.Z;
            return new Color4(r, g, b, xyz.W);
        }

        /// <summary>
        /// Converts RGB color values to XYZ color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value with the trisimulus values of X, Y, and Z in the corresponding element, and the W element with Alpha (a copy of the input's Alpha value).
        /// Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        /// <remarks>Uses the CIE XYZ colorspace.</remarks>
        public static Vector4 ToXYZ(Color4 rgb)
        {
            var x = (0.49f * rgb.R + 0.31f * rgb.G + 0.20f * rgb.B) / 0.17697f;
            var y = (0.17697f * rgb.R + 0.81240f * rgb.G + 0.01063f * rgb.B) / 0.17697f;
            var z = (0.00f * rgb.R + 0.01f * rgb.G + 0.99f * rgb.B) / 0.17697f;
            return new Vector4(x, y, z, rgb.A);
        }

        #endregion

        #region YUV

        /// <summary>
        /// Converts YCbCr color values to RGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="yuv">
        /// Color value to convert in Luma-Chrominance (YCbCr) aka YUV.
        /// The X element contains Luma (Y, 0.0 to 1.0), the Y element contains Blue-difference chroma (U, -0.5 to 0.5), the Z element contains the R-difference chroma (V, -0.5 to 0.5), and the W element contains the Alpha (which is copied to the output's Alpha value).
        /// </param>
        /// <remarks>Converts using ITU-R BT.601/CCIR 601 W(r) = 0.299 W(b) = 0.114 U(max) = 0.436 V(max) = 0.615.</remarks>
        public static Color4 FromYUV(Vector4 yuv)
        {
            var r = 1.0f * yuv.X + 0.0f * yuv.Y + 1.402f * yuv.Z;
            var g = 1.0f * yuv.X + -0.344136f * yuv.Y + -0.714136f * yuv.Z;
            var b = 1.0f * yuv.X + 1.772f * yuv.Y + 0.0f * yuv.Z;
            return new Color4(r, g, b, yuv.W);
        }

        /// <summary>
        /// Converts RGB color values to YUV color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value in Luma-Chrominance (YCbCr) aka YUV.
        /// The X element contains Luma (Y, 0.0 to 1.0), the Y element contains Blue-difference chroma (U, -0.5 to 0.5), the Z element contains the R-difference chroma (V, -0.5 to 0.5), and the W element contains the Alpha (a copy of the input's Alpha value).
        /// Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        /// <remarks>Converts using ITU-R BT.601/CCIR 601 W(r) = 0.299 W(b) = 0.114 U(max) = 0.436 V(max) = 0.615.</remarks>
        public static Vector4 ToYUV(Color4 rgb)
        {
            var y = 0.299f * rgb.R + 0.587f * rgb.G + 0.114f * rgb.B;
            var u = -0.168736f * rgb.R + -0.331264f * rgb.G + 0.5f * rgb.B;
            var v = 0.5f * rgb.R + -0.418688f * rgb.G + -0.081312f * rgb.B;
            return new Vector4(y, u, v, rgb.A);
        }

        #endregion

        #region HCY

        /// <summary>
        /// Converts HCY color values to RGB color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// </returns>
        /// <param name="hcy">
        /// Color value to convert in hue, chroma, luminance (HCY).
        /// The X element is Hue (H), the Y element is Chroma (C), the Z element is luminance (Y), and the W element is Alpha (which is copied to the output's Alpha value).
        /// Each has a range of 0.0 to 1.0.
        /// </param>
        public static Color4 FromHCY(Vector4 hcy)
        {
            var hue = hcy.X * 360.0f;
            var C = hcy.Y;
            var luminance = hcy.Z;

            var h = hue / 60.0f;
            var X = C * (1.0f - Math.Abs(h % 2.0f - 1.0f));

            float r, g, b;
            if (0.0f <= h && h < 1.0f)
            {
                r = C;
                g = X;
                b = 0.0f;
            }
            else if (1.0f <= h && h < 2.0f)
            {
                r = X;
                g = C;
                b = 0.0f;
            }
            else if (2.0f <= h && h < 3.0f)
            {
                r = 0.0f;
                g = C;
                b = X;
            }
            else if (3.0f <= h && h < 4.0f)
            {
                r = 0.0f;
                g = X;
                b = C;
            }
            else if (4.0f <= h && h < 5.0f)
            {
                r = X;
                g = 0.0f;
                b = C;
            }
            else if (5.0f <= h && h < 6.0f)
            {
                r = C;
                g = 0.0f;
                b = X;
            }
            else
            {
                r = 0.0f;
                g = 0.0f;
                b = 0.0f;
            }

            var m = luminance - (0.299f * r + 0.587f * g + 0.114f * b);
            return new Color4(r + m, g + m, b + m, hcy.W);
        }

        /// <summary>
        /// Converts RGB color values to HCY color values.
        /// </summary>
        /// <returns>
        /// Returns the converted color value.
        /// The X element is Hue (H), the Y element is Chroma (C), the Z element is luminance (Y), and the W element is Alpha (a copy of the input's Alpha value).
        /// Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        public static Vector4 ToHCY(Color4 rgb)
        {
            var M = Math.Max(rgb.R, Math.Max(rgb.G, rgb.B));
            var m = Math.Min(rgb.R, Math.Min(rgb.G, rgb.B));
            var C = M - m;

            float hue = 0.0f;
            if (C > 0f)
            {
                if (M == rgb.R)
                {
                    hue = ((rgb.G - rgb.B) / C) % 6.0f;
                }
                else if (M == rgb.G)
                {
                    hue = ((rgb.B - rgb.R) / C) + 2.0f;
                }
                else if (M == rgb.B)
                {
                    hue = ((rgb.R - rgb.G) / C) + 4.0f;
                }
            }

            hue /= 6.0f;
            if (hue < 0.0f)
                hue += 1.0f;

            var luminance = 0.299f * rgb.R + 0.587f * rgb.G + 0.114f * rgb.B;

            return new Vector4(hue, C, luminance, rgb.A);
        }

        #endregion
    }
}

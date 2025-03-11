using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Skia
{
    public enum CommonSpace
    {
        /// <summary>
        /// Height goes from -1 Top to 1 Bottom. The origin is located in the center.
        /// </summary>
        Normalized,

        /// <summary>
        /// Works with device independant pixels. One unit equals 100 actual DIP. The origin is located in the center.
        /// </summary>
        DIP,

        /// <summary>
        /// Works with device independant pixels. One unit equals 100 actual DIP. The origin is located at the top left.
        /// </summary>
        DIPTopLeft,

        /// <summary>
        /// Works with pixels. One unit equals 100 actual pixels. The origin is located at the top left.
        /// </summary>
        PixelTopLeft,
    };

    public enum PointShape
    {
        Square,
        Circle
    };

    public enum VerticalTextAlignment
    {
        Baseline,
        Top,
        Center,
        Bottom
    }

    public enum EncodedSkiaImageFormat
    {
        Jpeg = 3,
        Png = 4,
        Webp = 6,
    }
}

namespace SkiaSharp
{
    [Obsolete("Only for compatibility")]
    public enum SKTypefaceStyle
    {
        Normal = 0,
        Bold = 1,
        Italic = 2,
        BoldItalic = 3
    }
}

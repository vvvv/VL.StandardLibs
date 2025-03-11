using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Skia
{
    public static class TransitionUtils
    {
        public static SKTypeface FromFamilyName(string familyName, SKTypefaceStyle style)
        {
            switch (style)
            {
                case SKTypefaceStyle.Normal:
                    return SKTypeface.FromFamilyName(familyName, SKFontStyle.Normal);
                case SKTypefaceStyle.Bold:
                    return SKTypeface.FromFamilyName(familyName, SKFontStyle.Bold);
                case SKTypefaceStyle.Italic:
                    return SKTypeface.FromFamilyName(familyName, SKFontStyle.Italic);
                case SKTypefaceStyle.BoldItalic:
                    return SKTypeface.FromFamilyName(familyName, SKFontStyle.BoldItalic);
                default:
                    throw new NotImplementedException();
            }
        }

        [Obsolete]
        public static SKPaint SetIsVerticalText(this SKPaint paint, bool value)
        {
            AppHost.CurrentDefaultLogger.LogWarning("{name} no longer exists in 3.0 - find alternative", nameof(SetIsVerticalText));
            return paint;
        }

        [Obsolete]
        public static SKPaint SetDeviceKerningEnabled(this SKPaint paint, bool value)
        {
            AppHost.CurrentDefaultLogger.LogWarning("{name} no longer exists in 3.0 - find alternative", nameof(SetDeviceKerningEnabled));
            return paint;
        }

        [Obsolete($"Use {nameof(SKFont)} instead.")]
        public static SKFont GetFont(this SKPaint paint)
        {
            return GetFont(paint);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(GetFont))]
            static extern SKFont GetFont(SKPaint paint);
        }
    }
}

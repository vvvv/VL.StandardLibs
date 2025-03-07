using SixLabors.Fonts;
using System.Runtime.InteropServices;
using VL.Lib.Mathematics;
using VL.Lib.Text;
using VL.Lib.Collections;

namespace VL.ImGui
{
    /// <summary>
    /// Defines a font configuration.
    /// </summary>
    /// <param name="FamilyName">The font family name.</param>
    /// <param name="FontStyle">The font style.</param>
    /// <param name="Size">The size of the font in device independent hecto pixel (1 = 100 DIP).</param>
    /// <param name="Name">An optional name to use for this configuration.</param>
    /// <param name="GlyphRange">The glyph range.</param>
    /// <param name="CustomGlyphRange">Custom glyph range. If set <see cref="GlyphRange"/> will be ignored. A valid range is 1..0xFFFF</param>
    public record FontConfig(
        FontList FamilyName, 
        FontStyle FontStyle = FontStyle.Regular, 
        float Size = 0.16f, 
        string Name = "", 
        GlyphRange GlyphRange = default,
        Spread<Range<int>>? CustomGlyphRange = default
        )
    {
        public static readonly FontConfig? Default;

        public Spread<Range<int>>? CustomRanges { get; init; } = CheckValidRange(CustomGlyphRange);

        static FontConfig()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            {
                using var defaultTypeFace = System.Drawing.SystemFonts.MessageBoxFont ?? System.Drawing.SystemFonts.DefaultFont;
                Default = new FontConfig(new FontList(defaultTypeFace.FontFamily.Name));
            }
        }

        public override string ToString() => !string.IsNullOrEmpty(Name) ? Name : $"{FamilyName} {FontStyle} {Size}";

        internal IntPtr CustomGlyphRangePtr
        {
            get
            {
                if (customGlyphRange is null && CustomRanges != null && CustomRanges.Count > 0)
                {
                    var i = 0;
                    customGlyphRange = GC.AllocateArray<ushort>(CustomRanges.Count * 2 + 1, pinned: true);
                    foreach (var r in CustomRanges)
                    {
                        customGlyphRange[i++] = (ushort)r.From;
                        customGlyphRange[i++] = (ushort)r.To;
                    }
                    customGlyphRange[i++] = 0;
                }
                return customGlyphRange != null ? Marshal.UnsafeAddrOfPinnedArrayElement(customGlyphRange, 0) : default;
            }
        }
        ushort[]? customGlyphRange;

        static Spread<Range<int>>? CheckValidRange(Spread<Range<int>>? r)
        {
            if (r is null || r.Count == 0)
                return null;

            if (!HasValidRanges(r))
                throw new ArgumentOutOfRangeException(nameof(CustomRanges));

            return r;

            static bool HasValidRanges(Spread<Range<int>> r) => r.Count > 0 && r.All(IsValidRange);

            static bool IsValidRange(Range<int> r) => r.From > 0 && r.To > 0 && r.From <= r.To && r.To <= ushort.MaxValue;
        }
    }
}

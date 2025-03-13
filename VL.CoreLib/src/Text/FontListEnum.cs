using System;
using System.Collections.Generic;
using VL.Lib.Collections;
using Microsoft.Win32;
using System.Reactive.Linq;
using System.Linq;
using SixLabors.Fonts;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Graphics.DirectWrite;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

namespace VL.Lib.Text
{

    [Serializable]
    public class FontList : DynamicEnumBase<FontList, FontListDefinition>
    {
        public FontList(string value) : base(value)
        {
        }

        //this method needs to be imported in VL to set the default
        public static FontList CreateDefault()
        {
            //use method of base class if nothing special required
            return CreateDefaultBase();
        }
    }

    public class FontListDefinition : DynamicEnumDefinitionBase<FontListDefinition>
    {

        //return the current enum entries
        protected override IReadOnlyDictionary<string, object> GetEntries()
        {
            var fontFaces = new Dictionary<string, object>();

            var fonts = new List<string>();
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                GetFonts_DirectWrite(fonts);
            else if (OperatingSystem.IsWindowsVersionAtLeast(5))
                GetFonts_Gdi32(fonts);
            else
                GetFonts_Directory(fonts);

            foreach (var family in fonts)
                fontFaces[family] = family;

            return fontFaces;


        }

        [SupportedOSPlatform("windows5.0")]
        private static unsafe void GetFonts_Gdi32(List<string> fonts)
        {
            var hdc = PInvoke.GetDC(default);
            if (hdc.Value == 0)
                return;

            try
            {
                var lf = new LOGFONTW()
                {
                    lfCharSet = FONT_CHARSET.DEFAULT_CHARSET,
                    lfPitchAndFamily = 0,
                    lfFaceName = default
                };

                PInvoke.EnumFontFamiliesEx(hdc, &lf,
                    (font, textMetric, fontType, lParam) =>
                    {
                        if (valid_logfont_for_enum(font))
                            fonts.Add(font->lfFaceName.ToString());
                        return 1; // non-zero means continue

                    },
                    default,
                    default);
            }
            finally
            {
                PInvoke.ReleaseDC(default, hdc);
            }

            // From https://github.com/google/skia/blob/1f193df9b393d50da39570dab77a0bb5d28ec8ef/src/ports/SkFontHost_win.cpp#L2154
            static bool valid_logfont_for_enum(LOGFONTW* lf) {
                // TODO: Vector FON is unsupported and should not be listed.
                var faceName = lf->lfFaceName.AsReadOnlySpan();
                return
                    // Ignore implicit vertical variants.
                    faceName.Length > 0 && faceName[0] != '@'

                    // DEFAULT_CHARSET is used to get all fonts, but also implies all
                    // character sets. Filter assuming all fonts support ANSI_CHARSET.
                    && FONT_CHARSET.ANSI_CHARSET == lf->lfCharSet
                ;
            }
        }

        [SupportedOSPlatform("windows6.1")]
        private static unsafe void GetFonts_DirectWrite(List<string> fonts)
        {
            PInvoke.DWriteCreateFactory(DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED, typeof(IDWriteFactory).GUID, out var factory)
                .ThrowOnFailure();

            ((IDWriteFactory)factory).GetSystemFontCollection(out var fontCollection, checkForUpdates: true);

            for (uint i = 0; i < fontCollection.GetFontFamilyCount(); i++)
            {
                fontCollection.GetFontFamily(i, out var fontFamily);
                fontFamily.GetFamilyNames(out var names);
                var fontFamilyName = GetString(0, names);
                fonts.Add(fontFamilyName);
            }
        }

        [SupportedOSPlatform("windows6.1")]
        static unsafe string GetString(uint index, IDWriteLocalizedStrings localizedStrings)
        {
            localizedStrings.GetStringLength(index, out var length);
            fixed (char* c = stackalloc char[(int)length + 1])
            {
                localizedStrings.GetString(index, c, length + 1);
                return new string(c);
            }
        }

        private static void GetFonts_Directory(List<string> fonts)
        {
            foreach (var family in SystemFonts.Families.OrderBy(x => x.Name))
            {
                if (IsStyle(family.Name))
                    continue;

                fonts.Add(family.Name);
            }

            static bool IsStyle(string name)
            {
                return name.EndsWith(" Light")
                    || name.EndsWith(" Medium")
                    || name.EndsWith(" Semilight")
                    || name.EndsWith(" Semibold")
                    || name.EndsWith(" Bold");
            }
        }

        //inform the system that the enum has changed
        protected override IObservable<object> GetEntriesChangedObservable()
        {
            if (OperatingSystem.IsWindows())
            {
#pragma warning disable CA1416 // Validate platform compatibility
                return Observable.FromEventPattern(
                        h => SystemEvents.InstalledFontsChanged += h,
                        h => SystemEvents.InstalledFontsChanged -= h)
                    .Delay(TimeSpan.FromSeconds(3));
#pragma warning restore CA1416 // Validate platform compatibility
            }
            return Observable.Empty<object>();
        }

        public static FontPath GetFontPath(string familyName, FontStyle fontStyle)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                return GetFontPathDirectWrite(familyName, fontStyle);
            else if (OperatingSystem.IsWindowsVersionAtLeast(5))
                return default;
            else
                return default;
        }


        [SupportedOSPlatform("windows6.1")]
        private static unsafe FontPath GetFontPathDirectWrite(string familyName, FontStyle fontStyle)
        {
            PInvoke.DWriteCreateFactory(DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED, typeof(IDWriteFactory).GUID, out var unknown)
                .ThrowOnFailure();

            if (unknown is not IDWriteFactory factory)
                throw new InvalidCastException($"Returned interface {unknown} does not implement {nameof(IDWriteFactory)}");

            factory.GetSystemFontCollection(out var fontCollection, checkForUpdates: true);

            fontCollection.FindFamilyName(familyName, out var index, out var exists);
            if (!exists)
                return default;

            fontCollection.GetFontFamily(index, out var fontFamily);
            var (weight, stretch, style) = GetTriplet(fontStyle);
            fontFamily.GetMatchingFonts(weight, stretch, style, out var matchingFonts);

            matchingFonts.GetFont(0, out var font);
            font.CreateFontFace(out var fontFace);

            uint numberOfFiles = 0;
            fontFace.GetFiles(ref numberOfFiles, null);
            if (numberOfFiles != 1)
                return default;

            var fontFiles = new IDWriteFontFile[numberOfFiles];
            fontFace.GetFiles(ref numberOfFiles, fontFiles);
            fontFiles[0].GetReferenceKey(out var fontFileReferenceKey, out var fontFileReferenceKeySize);

            fontFiles[0].GetLoader(out var loader);

            if (loader is IDWriteLocalFontFileLoader localLoader)
            {
                localLoader.GetFilePathLengthFromKey(fontFileReferenceKey, fontFileReferenceKeySize, out var filePathLength);
                char* c = stackalloc char[(int)filePathLength + 1];
                localLoader.GetFilePathFromKey(fontFileReferenceKey, fontFileReferenceKeySize, c, filePathLength + 1);
                return new FontPath(new string(c), (int)fontFace.GetIndex());
            }

            return default;
        }

        static (DWRITE_FONT_WEIGHT weight, DWRITE_FONT_STRETCH stretch, DWRITE_FONT_STYLE style) GetTriplet(FontStyle fontStyle)
        {
            switch (fontStyle)
            {
                case FontStyle.Regular:
                    return (DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL, DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL, DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL);
                case FontStyle.Bold:
                    return (DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_BOLD, DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL, DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL);
                case FontStyle.Italic:
                    return (DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL, DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL, DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_ITALIC);
                case FontStyle.BoldItalic:
                    return (DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_BOLD, DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL, DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_ITALIC);
                default:
                    throw new NotImplementedException();
            }
        }

        public record struct FontPath(string Path, int Index)
        {
            public bool IsDefault => Path is null;
        }
    }
}

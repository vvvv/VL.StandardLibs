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
                names.GetStringLength(0, out var length);
                fixed (char* c = new char[length + 1])
                {
                    names.GetString(0, new PWSTR(c), length + 1);
                    fonts.Add(new string(c));
                }
            }

            Marshal.ReleaseComObject(factory);
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
    }
}

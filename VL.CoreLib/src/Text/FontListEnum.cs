using System;
using System.Collections.Generic;
using VL.Lib.Collections;
using Microsoft.Win32;
using System.Reactive.Linq;
using System.Linq;
using SixLabors.Fonts;

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
            foreach (FontFamily family in SystemFonts.Families.OrderBy(x => x.Name))
            {
                if (IsStyle(family.Name))
                    continue;

                fontFaces[family.Name] = family;
            }
            return fontFaces;

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

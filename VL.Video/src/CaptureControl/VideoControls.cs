using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Windows.Win32.Media.DirectShow;

namespace VL.Video.CaptureControl
{
    public sealed class VideoControls : IControls<VideoProcAmpProperty>
    {
        internal static readonly VideoControls Default = new VideoControls();

        internal VideoControls()
        {
            var values = Enum.GetValues(typeof(VideoProcAmpProperty));
            Properties = values
                .Cast<VideoProcAmpProperty>()
                .Select(v => new Property<VideoProcAmpProperty>(v, v.ToString().Substring("VideoProcAmp_".Length)))
                .ToImmutableArray();
        }

        internal readonly ImmutableArray<Property<VideoProcAmpProperty>> Properties;

        IEnumerable<Property<VideoProcAmpProperty>> IControls<VideoProcAmpProperty>.GetProperties() => Properties;
    }
}

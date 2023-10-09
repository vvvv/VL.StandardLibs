using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Windows.Win32.Media.DirectShow;

namespace VL.Video.CaptureControl
{
    public sealed class CameraControls : IControls<CameraControlProperty>
    {
        internal static readonly CameraControls Default = new CameraControls();

        internal CameraControls()
        {
            var values = Enum.GetValues(typeof(CameraControlProperty));
            Properties = values
                .Cast<CameraControlProperty>()
                .Select(v => new Property<CameraControlProperty>(v, v.ToString().Substring("CameraControl_".Length)))
                .ToImmutableArray();
        }

        internal readonly ImmutableArray<Property<CameraControlProperty>> Properties;

        IEnumerable<Property<CameraControlProperty>> IControls<CameraControlProperty>.GetProperties() => Properties;
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using VL.Core;
using VL.Core.Import;
using VL.Model;
using Windows.Win32.Media.DirectShow;

namespace VL.Video.CaptureControl
{
    [ProcessNode(Name = "CameraControls", Category = "Video", FragmentSelection = FragmentSelection.Explicit, Summary = "Controls camera parameters like zoom, exposure,...", Remarks = "Connects to the VideoIn node.\r\nNote that not all cameras will support all of the available properties")]
    public sealed class CameraControls : IControls<CameraControlProperty>
    {
        internal static readonly CameraControls Default = new CameraControls();

        [Fragment]
        public CameraControls()
        {
            var values = Enum.GetValues(typeof(CameraControlProperty));
            Properties = values
                .Cast<CameraControlProperty>()
                .Select(v => new Property<CameraControlProperty>(v, v.ToString().Substring("CameraControl_".Length)))
                .ToImmutableArray();
        }

        internal readonly ImmutableArray<Property<CameraControlProperty>> Properties;

        [Fragment]
        [return: Pin(Name = "Output")]
        public CameraControls Update(
            [Pin(Name = "Pan")] Optional<float> pan = default,
            [Pin(Name = "Tilt")] Optional<float> tilt = default,
            [Pin(Name = "Roll")] Optional<float> roll = default,
            [Pin(Name = "Zoom")] Optional<float> zoom = default,
            [Pin(Name = "Exposure")] Optional<float> exposure = default,
            [Pin(Name = "Iris")] Optional<float> iris = default,
            [Pin(Name = "Focus")] Optional<float> focus = default)
        {
            Set(CameraControlProperty.CameraControl_Pan, pan);
            Set(CameraControlProperty.CameraControl_Tilt, tilt);
            Set(CameraControlProperty.CameraControl_Roll, roll);
            Set(CameraControlProperty.CameraControl_Zoom, zoom);
            Set(CameraControlProperty.CameraControl_Exposure, exposure);
            Set(CameraControlProperty.CameraControl_Iris, iris);
            Set(CameraControlProperty.CameraControl_Focus, focus);
            return this;
        }

        void Set(CameraControlProperty property, Optional<float> value) => Properties[(int)property].Value = value;

        IEnumerable<Property<CameraControlProperty>> IControls<CameraControlProperty>.GetProperties() => Properties;
    }
}

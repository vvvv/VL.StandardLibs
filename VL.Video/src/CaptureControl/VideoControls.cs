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
    /// <summary>Holds optional values for video-processing controls such as brightness and contrast.</summary>
    [ProcessNode(Name = "VideoControls", Category = "Video", FragmentSelection = FragmentSelection.Explicit, Summary = "Controls video parameters like brightness, contrast,...", Remarks = "Connects to the VideoIn node.\r\nNote that not all cameras will support all of the available properties")]
    public sealed class VideoControls : IControls<VideoProcAmpProperty>
    {
        internal static readonly VideoControls Default = new VideoControls();

        /// <summary>Creates a control set with all values unset.</summary>
        [Fragment]
        public VideoControls()
        {
            var values = Enum.GetValues(typeof(VideoProcAmpProperty));
            Properties = values
                .Cast<VideoProcAmpProperty>()
                .Select(v => new Property<VideoProcAmpProperty>(v, v.ToString().Substring("VideoProcAmp_".Length)))
                .ToImmutableArray();
        }

        internal readonly ImmutableArray<Property<VideoProcAmpProperty>> Properties;

        /// <summary>Sets optional video-processing controls. Valid values and supported controls vary by device.</summary>
        /// <param name="brightness">Optional brightness value; units and range are device-specific.</param>
        /// <param name="contrast">Optional contrast value; units and range are device-specific.</param>
        /// <param name="hue">Optional hue value; units and range are device-specific.</param>
        /// <param name="saturation">Optional saturation value; units and range are device-specific.</param>
        /// <param name="sharpness">Optional sharpness value; units and range are device-specific.</param>
        /// <param name="gamma">Optional gamma value; units and range are device-specific.</param>
        /// <param name="colorEnable">Optional color-enable value; interpretation is device-specific.</param>
        /// <param name="whiteBalance">Optional white-balance value; units and range are device-specific.</param>
        /// <param name="backlightCompensation">Optional backlight-compensation value; interpretation is device-specific.</param>
        /// <param name="gain">Optional gain value; units and range are device-specific.</param>
        /// <returns>The updated video-control set.</returns>
        [Fragment]
        [return: Pin(Name = "Output")]
        public VideoControls Update(
            [Pin(Name = "Brightness")] Optional<float> brightness = default,
            [Pin(Name = "Contrast")] Optional<float> contrast = default,
            [Pin(Name = "Hue")] Optional<float> hue = default,
            [Pin(Name = "Saturation")] Optional<float> saturation = default,
            [Pin(Name = "Sharpness")] Optional<float> sharpness = default,
            [Pin(Name = "Gamma")] Optional<float> gamma = default,
            [Pin(Name = "ColorEnable")] Optional<float> colorEnable = default,
            [Pin(Name = "WhiteBalance")] Optional<float> whiteBalance = default,
            [Pin(Name = "BacklightCompensation")] Optional<float> backlightCompensation = default,
            [Pin(Name = "Gain")] Optional<float> gain = default)
        {
            Set(VideoProcAmpProperty.VideoProcAmp_Brightness, brightness);
            Set(VideoProcAmpProperty.VideoProcAmp_Contrast, contrast);
            Set(VideoProcAmpProperty.VideoProcAmp_Hue, hue);
            Set(VideoProcAmpProperty.VideoProcAmp_Saturation, saturation);
            Set(VideoProcAmpProperty.VideoProcAmp_Sharpness, sharpness);
            Set(VideoProcAmpProperty.VideoProcAmp_Gamma, gamma);
            Set(VideoProcAmpProperty.VideoProcAmp_ColorEnable, colorEnable);
            Set(VideoProcAmpProperty.VideoProcAmp_WhiteBalance, whiteBalance);
            Set(VideoProcAmpProperty.VideoProcAmp_BacklightCompensation, backlightCompensation);
            Set(VideoProcAmpProperty.VideoProcAmp_Gain, gain);
            return this;
        }

        void Set(VideoProcAmpProperty property, Optional<float> value) => Properties[(int)property].Value = value;

        IEnumerable<Property<VideoProcAmpProperty>> IControls<VideoProcAmpProperty>.GetProperties() => Properties;
    }
}

using System;
using VL.Core.CompilerServices;
using VL.Lib.Collections;

namespace VL.Video
{

    [Serializable]
    public class VideoCaptureDeviceEnumEntry : DynamicEnumBase<VideoCaptureDeviceEnumEntry, VideoCaptureDeviceEnum>
    {
        public VideoCaptureDeviceEnumEntry(string value) : base(value)
        {
        }

        [CreateDefault]
        public static VideoCaptureDeviceEnumEntry CreateDefault() => CreateDefaultBase("No video capture device found");
    }
}
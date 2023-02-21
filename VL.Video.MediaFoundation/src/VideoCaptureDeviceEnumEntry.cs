using System;
using VL.Lib.Collections;

namespace VL.Video.MediaFoundation
{

    [Serializable]
    public class VideoCaptureDeviceEnumEntry : DynamicEnumBase<VideoCaptureDeviceEnumEntry, VideoCaptureDeviceEnum>
    {
        public VideoCaptureDeviceEnumEntry(string value) : base(value)
        {
        }

        //this method needs to be imported in VL to set the default
        public static VideoCaptureDeviceEnumEntry CreateDefault() => CreateDefaultBase("No video capture device found");
    }
}
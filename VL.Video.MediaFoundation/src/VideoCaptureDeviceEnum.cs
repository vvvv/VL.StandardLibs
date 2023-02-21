using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using VL.Lib;
using VL.Lib.Collections;

namespace VL.Video.MediaFoundation
{
    /// <summary>
    /// Dynamic enum of available video input devices
    /// </summary>
    public class VideoCaptureDeviceEnum : DynamicEnumDefinitionBase<VideoCaptureDeviceEnum>
    {
        //return the current enum entries
        protected override IReadOnlyDictionary<string, object> GetEntries()
        {
            var devices = VideoCaptureHelpers.EnumerateVideoDevices();
            var result = new Dictionary<string, object>(devices.Length);

            // Add a default entry which makes it up to the system to select a device
            result.Add("Default", default(string));

            foreach (var device in devices)
            {
                var friendlyName = device.Get(CaptureDeviceAttributeKeys.FriendlyName);
                var symbolicLink = device.Get(CaptureDeviceAttributeKeys.SourceTypeVidcapSymbolicLink);
                var finalName = friendlyName;
                var j = 1;
                while (result.ContainsKey(finalName))
                {
                    finalName = friendlyName + " #" + j++;
                }
                result[finalName] = symbolicLink;
            }
            return result;
        }

        //inform the system that the enum has changed
        protected override IObservable<object> GetEntriesChangedObservable()
        {
            return HardwareChangedEvents.HardwareChanged;
        }

        //disable alphabetic sorting
        protected override bool AutoSortAlphabetically => false;
    }
}

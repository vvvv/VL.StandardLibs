using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using VL.Lib;
using VL.Lib.Collections;
using VL.Video.MF;
using static Windows.Win32.PInvoke;

namespace VL.Video
{
    /// <summary>
    /// Dynamic enum of available video input devices
    /// </summary>
    public class VideoCaptureDeviceEnum : DynamicEnumDefinitionBase<VideoCaptureDeviceEnum>
    {
        //return the current enum entries
        protected override unsafe IReadOnlyDictionary<string, object> GetEntries()
        {
            var result = new Dictionary<string, object>()
            {
                // Add a default entry which makes it up to the system to select a device
                { "Default", default(string) }
            };

            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            {
                using var devices = Utils.EnumerateVideoDevices();

                for (int i = 0; i < devices.Count; i++)
                {
                    var device = devices[i];
                    var friendlyName = Utils.GetString(device, in MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME);
                    var symbolicLink = Utils.GetString(device, in MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK);
                    var finalName = friendlyName;
                    var j = 1;
                    while (result.ContainsKey(finalName))
                    {
                        finalName = friendlyName + " #" + j++;
                    }
                    result[finalName] = symbolicLink;
                }
            }

            return result;
        }

        //inform the system that the enum has changed
        protected override IObservable<object> GetEntriesChangedObservable()
        {
            if (OperatingSystem.IsWindows())
                return HardwareChangedEvents.HardwareChanged;

            return Observable.Empty<object>();
        }

        //disable alphabetic sorting
        protected override bool AutoSortAlphabetically => false;
    }
}

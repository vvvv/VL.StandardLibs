using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.Collections;

namespace VL.Lib
{
    public class PlaybackDeviceEnumDefinition : DynamicEnumDefinitionBase<PlaybackDeviceEnumDefinition>
    {
        protected override IReadOnlyDictionary<string, object> GetEntries()
        {
            if (SystemDevice.LazyAudioController.IsValueCreated)
                return SystemDevice.AudioController.GetDevices(DeviceType.Playback, DeviceState.Active).ToDictionary(d => d.FullName, d => (object)d);
            else
                return SystemDevice.LoadingEnumEntry;
        }

        protected override IObservable<object> GetEntriesChangedObservable()
        {
            return SystemDevice.UpdateEnum;
        }

        //add this to get a node that can access the Instance from everywhere
        public static new PlaybackDeviceEnumDefinition Instance => DynamicEnumDefinitionBase<PlaybackDeviceEnumDefinition>.Instance;
    }

    [Serializable]
    public class PlaybackDeviceEnum : DynamicEnumBase<PlaybackDeviceEnum, PlaybackDeviceEnumDefinition>
    {
        public PlaybackDeviceEnum(string value) : base(value)
        {
        }

        //this method needs to be imported in VL to set the default
        public static PlaybackDeviceEnum CreateDefault()
        {
            //use method of base class if nothing special required
            return CreateDefaultBase();
        }
    }

    public class CaptureDeviceEnumDefinition : DynamicEnumDefinitionBase<CaptureDeviceEnumDefinition>
    {
        protected override IReadOnlyDictionary<string, object> GetEntries()
        {
            if (SystemDevice.LazyAudioController.IsValueCreated)
                return SystemDevice.AudioController.GetDevices(DeviceType.Capture, DeviceState.Active).ToDictionary(d => d.FullName, d => (object)d);
            else
                return SystemDevice.LoadingEnumEntry;
        }

        protected override IObservable<object> GetEntriesChangedObservable()
        {
            return SystemDevice.UpdateEnum;
        }

        //add this to get a node that can access the Instance from everywhere
        public static new CaptureDeviceEnumDefinition Instance => DynamicEnumDefinitionBase<CaptureDeviceEnumDefinition>.Instance;
    }

    [Serializable]
    public class CaptureDeviceEnum : DynamicEnumBase<CaptureDeviceEnum, CaptureDeviceEnumDefinition>
    {
        public CaptureDeviceEnum(string value) : base(value)
        {
        }

        //this method needs to be imported in VL to set the default
        public static CaptureDeviceEnum CreateDefault()
        {
            //use method of base class if nothing special required
            return CreateDefaultBase();
        }
    }
}

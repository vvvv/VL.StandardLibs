using AudioSwitcher.AudioApi.CoreAudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Lib
{
    public static class SystemDevice
    {
        static Lazy<IObservable<object>> lazyEnumUpdate = new Lazy<IObservable<object>>(SetupObservable);
        public static Lazy<CoreAudioController> LazyAudioController { get; } = new Lazy<CoreAudioController>(() => new CoreAudioController());
        public static CoreAudioController AudioController => LazyAudioController.Value;
        public static IObservable<object> UpdateEnum => lazyEnumUpdate.Value;

        static Lazy<IReadOnlyDictionary<string, object>> lazyEmptyEnum = new Lazy<IReadOnlyDictionary<string, object>>(() => new Dictionary<string, object>() { { "Loading...", null } });
        public static IReadOnlyDictionary<string, object> LoadingEnumEntry => lazyEmptyEnum.Value;

        static IObservable<object> SetupObservable()
        {
            var getController = Observable.Start(() => AudioController);
            var deviceChanged = getController.Select(c => c.AudioDeviceChanged).Switch();

            return Observable.Merge<object>(getController, deviceChanged);
        }


        public static bool SetPlaybackDevice(PlaybackDeviceEnum device)
        {
            var result = false;

            if (device.Tag is CoreAudioDevice audioDevice)
            {
                result = audioDevice.SetAsDefault();
            }

            return result;
        }

        public static bool SetCaptureDevice(CaptureDeviceEnum device)
        {
            var result = false;

            if (device.Tag is CoreAudioDevice audioDevice)
            {
                result = audioDevice.SetAsDefault();
            }

            return result;
        }

        public static bool SetCommunicationPlaybackDevice(PlaybackDeviceEnum device)
        {
            var result = false;

            if (device.Tag is CoreAudioDevice audioDevice)
            {
                result = audioDevice.SetAsDefaultCommunications();
            }

            return result;
        }

        public static bool SetCommunicationCaptureDevice(CaptureDeviceEnum device)
        {
            var result = false;

            if (device.Tag is CoreAudioDevice audioDevice)
            {
                result = audioDevice.SetAsDefaultCommunications();
            }

            return result;
        }
    }
}

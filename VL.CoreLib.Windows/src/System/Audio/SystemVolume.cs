using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System;
using System.Linq;

namespace VL.Lib
{
    public class SystemVolume
    {
        public float Volume
        {
            get => (float)SystemDevice.AudioController.DefaultPlaybackDevice.Volume;
            set
            {
                SystemDevice.AudioController.DefaultPlaybackDevice.SetVolumeAsync(Math.Max(0, Math.Min(value, 100)));
            }
        }
    }
}

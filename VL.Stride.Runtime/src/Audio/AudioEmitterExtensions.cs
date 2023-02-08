using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Audio;
using Stride.Engine;

namespace VL.Stride.Audio
{
    public static class AudioEmitterExtensions
    {
        public static Entity AddAudioEmitter(this Entity entity, out AudioEmitterComponent audioEmitter)
        {
            audioEmitter = new AudioEmitterComponent();
            entity.Add(audioEmitter);
            return entity;
        }

        /// <summary>
        /// Adds a sound to the audio emitter.
        /// </summary>
        /// <remarks>
        /// It uses the name of the sound as key for the sound controller. So set the Name property of the sound to something meaningful before. 
        /// </remarks>
        /// <param name="audioEmitter">The audio emitter.</param>
        /// <param name="sound">The sound.</param>
        /// <returns></returns>
        public static AudioEmitterComponent AddSound(this AudioEmitterComponent audioEmitter, Sound sound)
        {
            audioEmitter.Sounds[sound.Name] = sound;
            audioEmitter.AttachSound(sound);
            return audioEmitter;
        }

        public static AudioEmitterComponent GetSoundController(this AudioEmitterComponent audioEmitter, string key, out AudioEmitterSoundController soundController)
        {
            soundController = audioEmitter[key];
            return audioEmitter;
        }


        public static Entity GetSoundController(this Entity entity, string key, out AudioEmitterSoundController soundController)
        {
            soundController = entity.Get<AudioEmitterComponent>()[key];
            return entity;
        }

        public static Entity RemoveAudioEmitter(this Entity entity)
        {
            entity.Components.Remove<AudioEmitterComponent>();
            return entity;
        }

    }
}

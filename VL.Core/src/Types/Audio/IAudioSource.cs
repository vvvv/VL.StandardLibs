#nullable enable
using System;
using VL.Core;
using VL.Lib.Basics.Resources;

namespace VL.Lib.Basics.Audio
{
    public interface IAudioSource
    {
        /// <summary>
        /// Grabs the next audio frame from the source.
        /// </summary>
        /// <param name="sampleCount">The desired sample count.</param>
        /// <param name="sampleRate">The desired sample rate. If not set it's up to the source.</param>
        /// <param name="channelCount">The desired channel count. If not set it's up to the source.</param>
        /// <param name="interleaved">Whether the samples shall be interleaved. If not set it's up to the source.</param>
        /// <returns>The audio frame with the desired amount of samples and channels.</returns>
        IResourceProvider<AudioFrame> GrabAudioFrame(int sampleCount, Optional<int> sampleRate, Optional<int> channelCount, Optional<bool> interleaved);
    }
}
#nullable restore
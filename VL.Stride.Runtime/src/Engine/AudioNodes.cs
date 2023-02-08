using Stride.Audio;
using Stride.Engine;
using System.Collections.Generic;
using VL.Core;

namespace VL.Stride.Engine
{
    static class AudioNodes
    {
        //public static IEnumerable<IVLNodeDescription> GetNodeDescriptions(IVLNodeDescriptionFactory factory)
        //{
        //    Audio components
        //    var audioCategory = "Stride.Audio";

        //    yield return factory.NewComponentNode<AudioEmitterComponent>(audioCategory)
        //        .AddInput(nameof(AudioEmitterComponent.Sounds), x => x.SampleCount, (x, v) => x.SampleCount = v, 16)
        //        .WithEnabledPin();

        //    yield return factory.NewComponentNode<AudioListenerComponent>(audioCategory)
        //        .AddInput(nameof(LightShaftComponent.DensityFactor), x => x.DensityFactor, (x, v) => x.DensityFactor = v, 0.002f)
        //        .AddInput(nameof(LightShaftComponent.SampleCount), x => x.SampleCount, (x, v) => x.SampleCount = v, 16)
        //        .AddInput(nameof(LightShaftComponent.SeparateBoundingVolumes), x => x.SeparateBoundingVolumes, (x, v) => x.SeparateBoundingVolumes = v, true)
        //        .WithEnabledPin();

        //    yield return factory.NewNode<SoundInstance>(category: audioCategory, fragmented: true)
        //        .AddInput(nameof(SoundInstance.DynamicSoundSource))

        //    yield return factory.NewComponentNode<LightShaftBoundingVolumeComponent>(lightsCategory)
        //        .AddInput(nameof(LightShaftBoundingVolumeComponent.Model), x => x.Model, (x, v) => x.Model = v) // Ensure to check for change! Property throws event!
        //        .AddInput(nameof(LightShaftBoundingVolumeComponent.LightShaft), x => x.LightShaft, (x, v) => x.LightShaft = v) // Ensure to check for change! Property throws event!
        //        .WithEnabledPin();
        //}
    }
}
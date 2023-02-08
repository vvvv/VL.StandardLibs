#nullable enable
using System;
using VL.Lib.Basics.Resources;

namespace VL.Lib.Basics.Audio
{
    public sealed record AudioStream(IObservable<IResourceProvider<AudioFrame>> Frames);
}
#nullable restore
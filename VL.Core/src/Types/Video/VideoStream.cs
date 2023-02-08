#nullable enable
using System;
using VL.Lib.Basics.Resources;

namespace VL.Lib.Basics.Video
{
    public sealed record VideoStream(IObservable<IResourceProvider<VideoFrame>> Frames);
}
#nullable restore
#nullable enable
using Stride.Core.Mathematics;
using System;
using VL.Video.CaptureControl;

namespace VL.Video
{
    internal readonly record struct VideoCaptureConfig(
        string? DeviceSymbolicLink,
        Int2 PreferredSize,
        float PreferredFps,
        IObservable<CameraControls> CameraControls,
        IObservable<VideoControls> VideoControls);
}

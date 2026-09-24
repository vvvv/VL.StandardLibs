#nullable enable
using System.ComponentModel;
using Stride.Core.Mathematics;
using VL.Core.Import;
using VL.Lib.Basics.Video;
using VL.Lib.IO;
using VL.Model;

namespace VL.Video.ProcessNodes;

[ProcessNode(Name = "VideoPlayer", Category = "Video", FragmentSelection = FragmentSelection.Explicit, Summary = "Play videos from disk", Tags = "avi,wmv,mp4,h264,mjpeg,mpeg,dv,mov")]
public sealed class VideoPlayerFileNode
{
    private readonly VL.Video.VideoPlayer videoPlayer = new();

    [Fragment]
    public VideoPlayerFileNode()
    {
    }

    [Fragment]
    [return: Pin(Name = "Output")]
    public IVideoSource Update(
        [Pin(Name = "Filename"), DefaultValue(typeof(Path), "")] Path? filename,
        [Pin(Name = "Play"), DefaultValue(false)] bool play,
        [Pin(Name = "Rate"), DefaultValue(1f)] float rate,
        [Pin(Name = "Seek Time"), DefaultValue(0f)] float seekTime,
        [Pin(Name = "Seek"), DefaultValue(false)] bool seek,
        [Pin(Name = "Loop Start Time"), DefaultValue(0f)] float loopStartTime,
        [Pin(Name = "Loop End Time"), DefaultValue(-1f)] float loopEndTime,
        [Pin(Name = "Loop"), DefaultValue(false)] bool loop,
        [Pin(Name = "Volume"), DefaultValue(1f)] float volume,
        [Pin(Name = "Texture Size", Visibility = PinVisibility.Optional)] Int2 textureSize,
        [Pin(Name = "Source Bounds", Visibility = PinVisibility.Optional)] RectangleF? sourceBounds,
        [Pin(Name = "Border Color", Visibility = PinVisibility.Optional)] Color4? borderColor,
        [Pin(Name = "Use Linear Texture Format", Visibility = PinVisibility.Optional), DefaultValue(false)] bool useLinearTextureFormat,
        [Pin(Name = "Playing")] out bool playing,
        [Pin(Name = "Current Time")] out float currentTime,
        [Pin(Name = "Duration")] out float duration,
        [Pin(Name = "Ready State")] out ReadyState readyState,
        [Pin(Name = "Error Code")] out ErrorState errorCode)
    {
        var output = videoPlayer.Update(filename?.ToString() ?? string.Empty, play, rate, seekTime, seek, loopStartTime, loopEndTime, loop, volume, textureSize, sourceBounds, borderColor, useLinearTextureFormat);
        playing = videoPlayer.Playing;
        currentTime = videoPlayer.CurrentTime;
        duration = videoPlayer.Duration;
        readyState = videoPlayer.ReadyState;
        errorCode = videoPlayer.ErrorCode;
        return output;
    }
}

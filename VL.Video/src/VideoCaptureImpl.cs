#nullable enable
using System;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;

namespace VL.Video
{
    internal abstract class VideoCaptureImpl : IVideoPlayer
    {
        public abstract float ActualFPS { get; }

        public Action? DisposeAction { get; set; }

        public abstract string SupportedFormats { get; }

        public abstract IResourceProvider<VideoFrame>? GrabVideoFrame();

        public virtual void Dispose()
        {
            DisposeAction?.Invoke();
        }
    }
}
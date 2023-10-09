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

        public VideoCapture? VideoCapture { get; set; }

        public abstract string SupportedFormats { get; }

        public IResourceProvider<VideoFrame>? GrabVideoFrame()
        {
            if (VideoCapture!.Enabled)
                return DoGrabVideoFrame();
            return null;
        }

        protected abstract IResourceProvider<VideoFrame>? DoGrabVideoFrame();

        public virtual void Dispose()
        {
            DisposeAction?.Invoke();
        }
    }
}
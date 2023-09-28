#nullable enable
using System;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;

namespace VL.Video
{
    internal abstract class VideoPlayerImpl : IVideoPlayer
    {
        public abstract float CurrentTime { get; }
        public abstract float Duration { get; }
        public abstract ErrorState ErrorCode { get; protected set; }
        public abstract bool IsEnded { get; }
        public abstract NetworkState NetworkState { get; }
        public abstract bool Playing { get; }
        public abstract ReadyState ReadyState { get; }
        public Action? DisposeAction { get; set; }

        public virtual void Dispose()
        {
            DisposeAction?.Invoke();
        }

        public abstract IResourceProvider<VideoFrame>? GrabVideoFrame();
    }
}
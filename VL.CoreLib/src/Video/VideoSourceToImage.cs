#nullable enable
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;

namespace VL.Lib.Video
{
    public abstract class VideoSourceToImage<TImage> : IDisposable
    {
        protected readonly BlockingCollection<Action> workQueueForMainThread = new(boundedCapacity: 1);
        protected readonly BlockingCollection<IResourceHandle<TImage>> resultQueue = new(boundedCapacity: 1);

        private readonly SerialDisposable streamSubscription = new SerialDisposable();
        private readonly SerialDisposable imageSubscription = new SerialDisposable();

        private IVideoSource? videoSource;
        private int? changeTicket;
        private bool preferPush;
        private bool preferGpu;

        protected abstract VideoPlaybackContext Context { get; }

        private VideoPlaybackContext GetVideoPlaybackContext(bool preferGpu) => preferGpu ? Context : new VideoPlaybackContext(Context.FrameClock);

        protected abstract void OnPush(IResourceProvider<VideoFrame> videoFrameProvider, bool mipmapped);

        protected abstract void OnPull(IResourceProvider<VideoFrame>? videoFrameProvider, bool mipmapped);

        public TImage Update(IVideoSource? videoSource, TImage fallback, bool mipmapped, bool preferPush = true, bool preferGpu = true)
        {
            if (videoSource != this.videoSource || videoSource?.GetChangedTicket() != changeTicket || preferPush != this.preferPush || preferGpu != this.preferGpu)
            {
                this.videoSource = videoSource;
                this.changeTicket = videoSource?.GetChangedTicket();
                this.preferPush = preferPush;
                this.preferGpu = preferGpu;

                // Kill current stream
                streamSubscription.Disposable = null;

                var ctx = GetVideoPlaybackContext(preferGpu);
                if (preferPush)
                {
                    streamSubscription.Disposable = videoSource?.GetPushBasedStream(ctx)
                        .Subscribe(v => OnPush(v, mipmapped));
                }
                else
                {
                    streamSubscription.Disposable = videoSource?.GetPullBasedStream(ctx)
                        .Do(v => OnPull(v, mipmapped))
                        .GetEnumerator();
                }
            }

            if (streamSubscription.Disposable is IEnumerator enumerator)
                enumerator.MoveNext();

            if (workQueueForMainThread.TryTake(out var action))
                action();

            if (resultQueue.TryTake(out var newHandle))
                imageSubscription.Disposable = newHandle;

            if (imageSubscription.Disposable is IResourceHandle<TImage> handle)
                return handle.Resource;
            else
                return fallback;
        }

        public virtual void Dispose()
        {
            imageSubscription.Dispose();
            streamSubscription.Dispose();

            workQueueForMainThread.CompleteAdding();
            foreach (var action in workQueueForMainThread.GetConsumingEnumerable())
                action();

            resultQueue.CompleteAdding();
            foreach (var remaining in resultQueue.GetConsumingEnumerable())
                remaining.Dispose();
        }
    }
}
#nullable restore
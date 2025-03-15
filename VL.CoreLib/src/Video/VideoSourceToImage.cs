#nullable enable
using Microsoft.Extensions.Logging;
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
        protected readonly BlockingCollection<IResourceHandle<TImage?>> resultQueue = new(boundedCapacity: 1);

        private readonly SerialDisposable streamSubscription = new SerialDisposable();
        private readonly SerialDisposable imageSubscription = new SerialDisposable();

        private IVideoSource? videoSource;
        private int? changeTicket;
        private bool preferPush;
        private bool preferGpu;

        protected abstract VideoPlaybackContext Context { get; }

        private VideoPlaybackContext GetVideoPlaybackContext(bool preferGpu) => preferGpu ? Context : new VideoPlaybackContext(Context.FrameClock, Context.Logger);

        protected abstract void OnPush(IResourceProvider<VideoFrame> videoFrameProvider, bool mipmapped);

        protected abstract void OnPull(IResourceProvider<VideoFrame>? videoFrameProvider, bool mipmapped);

        public TImage Update(IVideoSource? videoSource, TImage fallback, bool mipmapped, bool preferPush = true, bool preferGpu = true)
        {
            if (videoSource != this.videoSource || videoSource?.GetChangedTicket() != changeTicket || preferPush != this.preferPush || preferGpu != this.preferGpu)
            {
                this.videoSource = videoSource;
                this.preferPush = preferPush;
                this.preferGpu = preferGpu;

                // Kill current stream
                streamSubscription.Disposable = null;

                // Fetch new ticket after the source has been disposed (it might have incremented it)
                this.changeTicket = videoSource?.GetChangedTicket();

                var ctx = GetVideoPlaybackContext(preferGpu);
                if (preferPush)
                {
                    streamSubscription.Disposable = videoSource?.GetPushBasedStream(ctx)
                        .Finally(() => imageSubscription.Disposable = null)
                        .Subscribe(v => OnPush(v, mipmapped), onError: e => Context.Logger.LogError(e, "Error in video stream."));
                }
                else
                {
                    // We run single threaded, we can therefor safe resources by releasing the image before grabbing a new one
                    streamSubscription.Disposable = videoSource?.GetPullBasedStream(ctx, beforeGrab: () => imageSubscription.Disposable = null)
                        .Do(v => OnPull(v, mipmapped))
                        .Finally(() => imageSubscription.Disposable = null)
                        .GetEnumerator();
                }
            }

            if (streamSubscription.Disposable is IEnumerator enumerator)
                enumerator.MoveNext();

            if (resultQueue.TryTake(out var newHandle))
                imageSubscription.Disposable = newHandle;

            if (imageSubscription.Disposable is IResourceHandle<TImage?> handle)
                return handle.Resource ?? fallback;
            else
                return fallback;
        }

        public virtual void Dispose()
        {
            imageSubscription.Dispose();
            streamSubscription.Dispose();

            resultQueue.CompleteAdding();
            foreach (var remaining in resultQueue.GetConsumingEnumerable())
                remaining.Dispose();
        }
    }
}
#nullable restore
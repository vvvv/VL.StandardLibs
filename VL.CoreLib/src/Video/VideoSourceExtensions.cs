#nullable enable
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;

namespace VL.Lib.Video
{
    public static class VideoSourceExtensions
    {
        public static int GetChangedTicket(this IVideoSource videoSource) => videoSource is IVideoSource2 v2 ? v2.ChangedTicket : 0;

        public static IEnumerable<IResourceProvider<VideoFrame>?> GetPullBasedStream(this IVideoSource videoSource, VideoPlaybackContext ctx, Action? beforeGrab = null)
        {
            if (videoSource is IVideoSource2 videoSource2)
                return videoSource2.GetPullBasedStream(ctx, beforeGrab);

            return EnumerableEx.Create(ProduceFrames);

            IEnumerator<IResourceProvider<VideoFrame>?> ProduceFrames()
            {
                while (true)
                {
                    beforeGrab?.Invoke();
                    yield return videoSource.GrabVideoFrame();
                }
            }
        }

        private static IEnumerable<IResourceProvider<VideoFrame>?> GetPullBasedStream(this IVideoSource2 videoSource, VideoPlaybackContext ctx, Action? beforeGrab = null)
        {
            return EnumerableEx.Create(ProduceFrames);

            IEnumerator<IResourceProvider<VideoFrame>?> ProduceFrames()
            {
                using var player = videoSource.Start(ctx);
                if (player is null)
                    yield break;

                while (true)
                {
                    beforeGrab?.Invoke();
                    yield return player.GrabVideoFrame();
                }
            }
        }

        public static IObservable<IResourceProvider<VideoFrame>> GetPushBasedStream(this IVideoSource videoSource, VideoPlaybackContext ctx)
        {
            if (videoSource is IVideoSource2 videoSource2)
                return videoSource2.GetPushBasedStream(ctx);

            return Observable.Create<IResourceProvider<VideoFrame>>(
                subscribeAsync: async (observer, token) =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        if (videoSource.TryGrabVideoFrame(ctx, out var frame))
                        {
                            if (frame != null)
                            {
                                observer.OnNext(frame);
                                await Task.Yield();
                            }
                            else
                            {
                                // To prevent CPU going to 100%
                                await Task.Delay(1);
                            }
                        }
                        else
                        {
                            // Give it a break
                            await Task.Delay(100, token);
                        }
                    }
                })
                .SubscribeOn(Scheduler.Default);
        }

        private static IObservable<IResourceProvider<VideoFrame>> GetPushBasedStream(this IVideoSource2 videoSource, VideoPlaybackContext ctx)
        {
            var finishedEvent = new ManualResetEvent(false);
            return Observable.Create<IResourceProvider<VideoFrame>>(
                subscribeAsync: async (observer, token) =>
                {
                    try
                    {
                        using var player = videoSource.Start(ctx);

                        if (player is null)
                            return;

                        while (!token.IsCancellationRequested)
                        {
                            if (player.TryGrabVideoFrame(ctx, out var frame))
                            {
                                if (frame != null)
                                {
                                    observer.OnNext(frame);
                                    await Task.Yield();
                                }
                                else
                                {
                                    // To prevent CPU going to 100%
                                    await Task.Delay(1);
                                }
                            }
                            else
                            {
                                // Give it a break
                                await Task.Delay(100, token);
                            }
                        }
                    }
                    finally
                    {
                        // Only works like this. Doesn't work with RX Finally operator!
                        finishedEvent.Set();
                    }
                })
                .SubscribeOn(Scheduler.Default)
                .Finally(() =>
                {
                    finishedEvent.WaitOne();
                    finishedEvent.Dispose();
                });
        }

        private static bool TryGrabVideoFrame(this IVideoPlayer player, VideoPlaybackContext context, out IResourceProvider<VideoFrame>? frame)
        {
            try
            {
                frame = player.GrabVideoFrame();
                return true;
            }
            catch (Exception e)
            {
                context.Logger.LogError(e, "Error while grabbing video frame.");
                frame = null;
                return false;
            }
        }

        private static bool TryGrabVideoFrame(this IVideoSource source, VideoPlaybackContext context, out IResourceProvider<VideoFrame>? frame)
        {
            try
            {
                frame = source.GrabVideoFrame();
                return true;
            }
            catch (Exception e)
            {
                context.Logger.LogError(e, "Error while grabbing video frame.");
                frame = null;
                return false;
            }
        }
    }
}

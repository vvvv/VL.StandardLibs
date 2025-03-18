#nullable enable
using SkiaSharp;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using VL.Core;
using VL.Lib.Animation;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;

namespace VL.Skia.Video
{
    public sealed class VideoStreamToSKImage : IDisposable
    {
        private readonly SerialDisposable imageStreamSubscription = new SerialDisposable();
        private readonly SerialDisposable latestSubscription = new SerialDisposable();
        private readonly SerialDisposable currentSubscription = new SerialDisposable();

        private readonly RenderContext renderContext;
        private readonly VideoPlaybackContext ctx;

        private VideoStream? videoStream;
        private IResourceProvider<SKImage?>? current, latest;

        public VideoStreamToSKImage(NodeContext nodeContext)
        {
            renderContext = RenderContext.ForCurrentApp();

            var frameClock = AppHost.Current.Services.GetRequiredService<IFrameClock>();
            if (renderContext.EglContext.Display.TryGetD3D11Device(out var d3dDevice))
                ctx = new VideoPlaybackContext(frameClock, nodeContext.GetLogger(), d3dDevice, GraphicsDeviceType.Direct3D11, renderContext.UseLinearColorspace);
            else
                ctx = new VideoPlaybackContext(frameClock, nodeContext.GetLogger());
        }

        public unsafe VideoStream? VideoStream 
        {
            get => videoStream;
            set
            {
                if (value != videoStream)
                {
                    videoStream = value;

                    imageStreamSubscription.Disposable = videoStream?.Frames
                        ?.Do(provider =>
                        {
                            var skImageProvider = provider
                                .ToSkImage(renderContext, mipmapped: false)
                                .ShareInParallel();

                            var handle = skImageProvider.GetHandle(); // Upload the texture

                            // Exchange provider
                            lock (this)
                            {
                                latest = skImageProvider;
                                latestSubscription.Disposable = handle;
                            }
                        })
                        .Finally(() =>
                        {
                            lock (this)
                            {
                                latest = null;
                                latestSubscription.Disposable = null;
                            }
                        })
                        .Subscribe();
                }
            }
        }

        public IResourceProvider<SKImage?>? Provider
        {
            get
            {
                lock (this)
                {
                    var latest = this.latest;
                    if (latest != current)
                    {
                        current = latest;
                        currentSubscription.Disposable = current?.GetHandle();
                    }
                    return current;
                }
            }
        }

        public void Dispose()
        {
            latestSubscription.Dispose();
            currentSubscription.Dispose();
            imageStreamSubscription.Dispose();
        }
    }
}
#nullable restore
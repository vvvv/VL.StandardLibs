#nullable enable
using Stride.Core;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;

namespace VL.Stride.Video
{
    public sealed class VideoStreamToTexture : IDisposable
    {
        private readonly SerialDisposable imageStreamSubscription = new SerialDisposable();
        private readonly SerialDisposable latestSubscription = new SerialDisposable();
        private readonly SerialDisposable currentSubscription = new SerialDisposable();
        private readonly RenderContext renderContext;

        private VideoStream? videoStream;
        private IResourceProvider<Texture>? current, latest;

        public VideoStreamToTexture()
        {
            var game = AppHost.Current.Services.GetRequiredService<Game>();
            renderContext = RenderContext.GetShared(game.Services);
        }

        public unsafe VideoStream? VideoStream 
        {
            get => videoStream;
            set
            {
                if (value != videoStream)
                {
                    videoStream = value;

                    imageStreamSubscription.Disposable = value?.Frames
                        .Do(provider =>
                        {
                            var textureProvider = provider.ToTexture(renderContext).ShareInParallel();
                            var handle = textureProvider.GetHandle(); // Upload the texture

                            // Exchange provider
                            lock (this)
                            {
                                latest = textureProvider;
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

        public IResourceProvider<Texture> TextureProvider
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
                    return current ?? ResourceProvider.Default<Texture>.GetInstance(default!);
                }
            }
        }

        public void Dispose()
        {
            imageStreamSubscription.Dispose();
            latestSubscription.Dispose();
            currentSubscription.Dispose();
        }
    }
}
#nullable restore
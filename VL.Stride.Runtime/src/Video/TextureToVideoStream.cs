using CommunityToolkit.HighPerformance;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using MapMode = Stride.Graphics.MapMode;
using StridePixelFormat = Stride.Graphics.PixelFormat;

namespace VL.Stride.Video
{
    public sealed class TextureToVideoStream : RendererBase
    {
        private readonly SynchronizationContext synchronizationContext = SynchronizationContext.Current;
        private readonly Queue<(Texture texture, string metadata)> textureDownloads = new Queue<(Texture texture, string metadata)>();
        private readonly Subject<IResourceProvider<VideoFrame>> frames = new Subject<IResourceProvider<VideoFrame>>();
        private readonly AppHost appHost;
        private readonly CompositeDisposable subscriptions;
        private readonly SerialDisposable texturePoolSubscription;

        public TextureToVideoStream()
        {
            appHost = AppHost.Current;
            subscriptions = new CompositeDisposable()
            {
                (texturePoolSubscription = new SerialDisposable()),
            };
            VideoStream = new VideoStream(frames);
        }

        public Texture Texture { get; set; }

        public string Metadata { get; set; }

        public VideoStream VideoStream { get; }

        /// <inheritdoc />
        protected override void DrawCore(RenderDrawContext context)
        {
            var texture = Texture;
            if (texture is null)
            {
                texturePoolSubscription.Disposable = null;
                return;
            }

            // Workaround: Re-install the app host - should be done by vvvv itself in the render callback
            using var _ = appHost.MakeCurrentIfNone();

            var texturePool = GetTexturePool(context.GraphicsDevice, texture);

            {
                // Request copy
                var stagingTexture = texturePool.Rent();
                context.CommandList.Copy(texture, stagingTexture);
                textureDownloads.Enqueue((stagingTexture, Metadata));
            }

            {
                // Download recently staged
                var (stagedTexture, stagedMetadata) = textureDownloads.Peek();
                var doNotWait = textureDownloads.Count <= 8 /* Under normal scenarios we shouldn't reach this limit */;
                var commandList = context.CommandList;
                var mappedResource = commandList.MapSubresource(stagedTexture, 0, MapMode.Read, doNotWait);
                var data = mappedResource.DataBox;
                if (!data.IsEmpty)
                {
                    // Dequeue
                    textureDownloads.Dequeue();

                    try
                    {
                        var (memoryOwner, videoFrame) = CreateVideoFrame(stagedTexture, data, stagedMetadata);

                        var videoFrameProvider = ResourceProvider.Return(videoFrame, ReleaseVideoFrame);

                        using (videoFrameProvider.GetHandle())
                        {
                            // Push it downstream
                            frames.OnNext(videoFrameProvider);
                        }

                        void ReleaseVideoFrame(VideoFrame videoFrame)
                        {
                            if (SynchronizationContext.Current != synchronizationContext)
                                synchronizationContext.Post(x => ReleaseVideoFrame((VideoFrame)x), videoFrame);
                            else
                            {
                                memoryOwner.Dispose();
                                if (!IsDisposed)
                                    commandList.UnmapSubresource(mappedResource);
                                texturePool.Return(stagedTexture);
                            }
                        }
                    }
                    catch
                    {
                        commandList.UnmapSubresource(mappedResource);
                        texturePool.Return(stagedTexture);
                        throw;
                    }
                }
            }
        }

        static (IDisposable memoryOwner, VideoFrame videoFrame) CreateVideoFrame(Texture texture, DataBox data, string metadata)
        {
            switch (texture.Format)
            {
                //case StridePixelFormat.R8_UNorm: return VLPixelFormat.R8;
                //case StridePixelFormat.R16_UNorm: return VLPixelFormat.R16;
                //case StridePixelFormat.R32_Float: return VLPixelFormat.R32F;
                case StridePixelFormat.R8G8B8A8_UNorm: return CreateVideoFrame<RgbaPixel>(texture, data, metadata);
                case StridePixelFormat.R8G8B8A8_UNorm_SRgb: return CreateVideoFrame<RgbaPixel>(texture, data, metadata);
                case StridePixelFormat.B8G8R8X8_UNorm: return CreateVideoFrame<BgrxPixel>(texture, data, metadata);
                case StridePixelFormat.B8G8R8X8_UNorm_SRgb: return CreateVideoFrame<BgrxPixel>(texture, data, metadata);
                case StridePixelFormat.B8G8R8A8_UNorm: return CreateVideoFrame<BgraPixel>(texture, data, metadata);
                case StridePixelFormat.B8G8R8A8_UNorm_SRgb: return CreateVideoFrame<BgraPixel>(texture, data, metadata);
                case StridePixelFormat.R16G16B16A16_Float: return CreateVideoFrame<Rgba16fPixel>(texture, data, metadata);
                //case StridePixelFormat.R32G32_Float: return VLPixelFormat.R32G32F;
                case StridePixelFormat.R32G32B32A32_Float: return CreateVideoFrame<Rgba32fPixel>(texture, data, metadata);
                default:
                    throw new Exception("Unsupported pixel format");
            }
        }

        static (IMemoryOwner<T> memoryOwner, VideoFrame<T> videoFrame) CreateVideoFrame<T>(Texture texture, DataBox data, string metadata)
            where T : unmanaged, IPixel
        {
            var memoryOwner = new UnmanagedMemoryManager<T>(data.DataPointer, data.SlicePitch);
            var pitch = data.RowPitch - texture.Width * Unsafe.SizeOf<T>();
            var videoFrame = new VideoFrame<T>(memoryOwner.Memory.AsMemory2D(0, texture.Height, texture.Width, pitch), metadata);
            return (memoryOwner, videoFrame);
        }

        private TexturePool GetTexturePool(GraphicsDevice graphicsDevice, Texture texture)
        {
            return TexturePool.Get(graphicsDevice, texture.Description.ToStagingDescription())
                .Subscribe(texturePoolSubscription);
        }

        protected override void Destroy()
        {
            while (textureDownloads.Count > 0)
                textureDownloads.Dequeue().texture.Dispose();

            subscriptions.Dispose();

            base.Destroy();
        }
    }
}

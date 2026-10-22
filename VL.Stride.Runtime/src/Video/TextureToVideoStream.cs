using CommunityToolkit.HighPerformance;
using Stride.Engine;
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
using VL.Stride.Engine;
using MapMode = Stride.Graphics.MapMode;
using StridePixelFormat = Stride.Graphics.PixelFormat;

[assembly: ImportType(typeof(VL.Stride.Video.TextureToVideoStream))]

namespace VL.Stride.Video
{
    /// <summary>
    /// Converts a texture into a video stream by copying it to staging textures and reading them back from the GPU asynchronously.
    /// </summary>
    /// <remarks>
    /// The frame rate pins only label the produced frames, no frame rate conversion takes place.
    /// If changed, make sure the values match the main loop settings of vvvv.
    /// </remarks>
    [ProcessNode(Category = "Stride.Video")]
    public sealed class TextureToVideoStream : RendererBase
    {
        record struct MetadataAndFramerate(string Metadata, (int N, int D) FrameRate);

        private readonly SynchronizationContext synchronizationContext = SynchronizationContext.Current;
        private readonly Queue<(Texture texture, MetadataAndFramerate metadata)> textureDownloads = new();
        private readonly Subject<IResourceProvider<VideoFrame>> frames = new Subject<IResourceProvider<VideoFrame>>();
        private readonly AppHost appHost;
        private readonly CompositeDisposable subscriptions;
        private readonly SerialDisposable texturePoolSubscription;
        private readonly SchedulerSystem schedulerSystem;

        private Texture texture;
        private MetadataAndFramerate metadata;
        private VideoStream videoStream;

        public TextureToVideoStream()
        {
            appHost = AppHost.Current;
            subscriptions = new CompositeDisposable()
            {
                (texturePoolSubscription = new SerialDisposable()),
            };
            videoStream = new VideoStream(frames);
            schedulerSystem = appHost.Services.GetRequiredService<Game>().Services.GetService<SchedulerSystem>();
        }

        /// <param name="input">The texture to stream. If null, no frame is produced.</param>
        /// <param name="metadata">Optional metadata attached to each video frame.</param>
        /// <param name="frameRateN">Numerator of the frame rate. Must match the main loop settings of vvvv, no conversion is done.</param>
        /// <param name="frameRateD">Denominator of the frame rate. Must match the main loop settings of vvvv, no conversion is done.</param>
        /// <returns>The stream of video frames. Frames arrive with a delay of a few updates due to the asynchronous readback.</returns>
        [return: Pin(Name = "Output")]
        public VideoStream Update(
            Texture input,
            string metadata,
            int frameRateN = Constants.DefaultFrameRateN, 
            int frameRateD = Constants.DefaultFrameRateD)
        {
            texture = input;
            this.metadata = new MetadataAndFramerate(metadata, (frameRateN, frameRateD));

            schedulerSystem.Schedule(this);

            return videoStream;
        }

        /// <inheritdoc />
        protected override void DrawCore(RenderDrawContext context)
        {
            var texture = this.texture;
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
                textureDownloads.Enqueue((stagingTexture, metadata));
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

        static (IDisposable memoryOwner, VideoFrame videoFrame) CreateVideoFrame(Texture texture, DataBox data, MetadataAndFramerate metadata)
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

        static (IMemoryOwner<T> memoryOwner, VideoFrame<T> videoFrame) CreateVideoFrame<T>(Texture texture, DataBox data, MetadataAndFramerate metadata)
            where T : unmanaged, IPixel
        {
            var memoryOwner = new UnmanagedMemoryManager<T>(data.DataPointer, data.SlicePitch);
            var pitch = data.RowPitch - texture.Width * Unsafe.SizeOf<T>();
            var videoFrame = new VideoFrame<T>(memoryOwner.Memory.AsMemory2D(0, texture.Height, texture.Width, pitch), metadata.Metadata, FrameRate: metadata.FrameRate);
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

#nullable enable
using SkiaSharp;
using System;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using VL.Skia.Egl;

namespace VL.Skia.Video
{
    static class VideoUtils
    {
        public static IResourceProvider<SKImage?> ToSkImage(this IResourceProvider<VideoFrame> provider, RenderContext renderContext, bool mipmapped)
        {
            return provider.GetHandle().ToSkImage(renderContext, mipmapped);
        }

        public static IResourceProvider<SKImage?> ToSkImage(this IResourceHandle<VideoFrame> handle, RenderContext renderContext, bool mipmapped)
        {
            var videoFrame = handle.Resource;
            if (videoFrame.TryGetTexture(out var texture))
            {
                return ResourceProvider.Return(handle, h => h.Dispose())
                    .Bind(_ => 
                        // Defer operator defers access to handle.Resource property so we can assume it will happen on the render thread where we need to be.
                        ResourceProvider.Defer(() =>
                            ResourceProvider.NewPooledPerApp(texture,
                                factory: texture => FromTexture(texture, renderContext),
                                delayDisposalInMilliseconds: 1000 /* Keep the wrappers in memory for a bit */)));
            }
            else
            {
                var image = FromMemory(videoFrame, renderContext, mipmapped);
                return ResourceProvider.Return(image, (image, handle), x =>
                {
                    x.image?.Dispose();
                    x.handle.Dispose();
                });
            }
        }

        private static unsafe SKImage FromMemory(VideoFrame videoFrame, RenderContext renderContext, bool mipmapped)
        {
            if (!videoFrame.TryGetMemory(out var memory))
                throw new NotImplementedException();

            var skInfo = new SKImageInfo(videoFrame.Width, videoFrame.Height, videoFrame.PixelFormat.ToSkColorType(), SKAlphaType.Unpremul);
            using var handle = memory.Pin();
            using var pixmap = new SKPixmap(skInfo, new IntPtr(handle.Pointer), videoFrame.RowLengthInBytes);
            return SKImage.FromPixels(pixmap, (_, c) => handle.Dispose());

            // Upload to GPU - Doesn't work, always returns null, recheck with new Skia release.
            //return image.ToTextureImage(renderContext.SkiaContext, mipmapped);
        }

        private static SKImage FromTexture(VideoTexture texture, RenderContext renderContext)
        {
            using var _ = renderContext.MakeCurrent(forRendering: false);
            return D3D11Utils.TextureToSKImage(renderContext, texture.nativePointer);
        }
    }
}

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
                                delayDisposalInMilliseconds: 1000 /* Keep the wrappers in memory for a bit */)
                            .Bind(imageHandle => imageHandle?.Image)));
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

        private static TextureImage? FromTexture(VideoTexture texture, RenderContext renderContext)
        {
            using var _ = renderContext.MakeCurrent(forRendering: false);

            var eglContext = renderContext.EglContext;

            using var eglImage = eglContext.CreateImageFromD3D11Texture(texture.NativePointer);

            uint textureId = 0;
            NativeGles.glGenTextures(1, ref textureId);

            // We need to restore the currently bound texture (https://github.com/devvvvs/vvvv/issues/5925)
            NativeGles.glGetIntegerv(NativeGles.GL_TEXTURE_BINDING_2D, out var currentTextureId);
            NativeGles.glBindTexture(NativeGles.GL_TEXTURE_2D, textureId);
            NativeGles.glEGLImageTargetTexture2DOES(NativeGles.GL_TEXTURE_2D, eglImage);
            NativeGles.glBindTexture(NativeGles.GL_TEXTURE_2D, (uint)currentTextureId);

            var colorType = texture.PixelFormat.ToSkColorType();
            var glInfo = new GRGlTextureInfo(
                id: textureId,
                target: NativeGles.GL_TEXTURE_2D,
                format: colorType.ToGlSizedFormat());

            using var backendTexture = new GRBackendTexture(
                width: texture.Width,
                height: texture.Height,
                mipmapped: false,
                glInfo: glInfo);

            var image = SKImage.FromTexture(
                renderContext.SkiaContext,
                backendTexture,
                GRSurfaceOrigin.TopLeft,
                colorType,
                SKAlphaType.Premul,
                // TODO: Check this
                colorspace: /*renderContext.UseLinearColorspace ? SKColorSpace.CreateSrgbLinear() : */SKColorSpace.CreateSrgb());

            if (image is null)
                return null;

            return new TextureImage(image, renderContext, textureId);
        }

        sealed class TextureImage : IDisposable
        {
            private readonly RenderContext renderContext;
            private uint textureId;

            public TextureImage(SKImage image, RenderContext renderContext, uint textureId)
            {
                if (image is null) throw new ArgumentNullException(nameof(image));
                Image = image;
                this.renderContext = renderContext;
                this.textureId = textureId;
            }

            public SKImage Image { get; }

            public void Dispose()
            {
                //renderContext.MakeCurrent();

                Image.Dispose();

                NativeGles.glDeleteTextures(1, ref textureId);
            }
        }
    }
}

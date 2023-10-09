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
        public static IResourceProvider<SKImage> ToSkImage(this IResourceProvider<VideoFrame> provider, RenderContext renderContext, bool mipmapped)
        {
            return provider.GetHandle().ToSkImage(renderContext, mipmapped);
        }

        public static IResourceProvider<SKImage> ToSkImage(this IResourceHandle<VideoFrame> handle, RenderContext renderContext, bool mipmapped)
        {
            var videoFrame = handle.Resource;
            if (videoFrame.TryGetTexture(out var texture))
            {
                // Tie the lifetime of the Skia image to the texture object (which might be pooled)
                var image = texture.Get<TextureImage>() ?? texture.Attach(FromTexture(texture, renderContext));
                // But hold on to the video frame to prevent the texture object from being used for another frame
                return ResourceProvider.Return(image.Image, handle, handle => handle.Dispose());
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

        public static bool HasAttachedSkImage(this VideoFrame videoFrame)
        {
            return videoFrame.TryGetTexture(out var videoTexture) && videoTexture.Get<SKImage>() != null;
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

        private static TextureImage FromTexture(VideoTexture texture, RenderContext renderContext)
        {
            renderContext.MakeCurrent();

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

            var result = default(TextureImage);

            var image = SKImage.FromTexture(
                renderContext.SkiaContext,
                backendTexture,
                GRSurfaceOrigin.TopLeft,
                colorType,
                SKAlphaType.Premul,
                // TODO: Check this
                colorspace: /*renderContext.UseLinearColorspace ? SKColorSpace.CreateSrgbLinear() : */SKColorSpace.CreateSrgb());

            return result = new(image, renderContext, textureId);
        }

        sealed class TextureImage : IDisposable
        {
            private readonly RenderContext renderContext;
            private uint textureId;

            public TextureImage(SKImage image, RenderContext renderContext, uint textureId)
            {
                Image = image;
                this.renderContext = renderContext;
                this.textureId = textureId;

                renderContext.AddRef();
            }

            public SKImage Image { get; }

            public void Dispose()
            {
                renderContext.MakeCurrent();

                Image.Dispose();

                NativeGles.glDeleteTextures(1, ref textureId);

                renderContext.Release();
            }
        }
    }
}

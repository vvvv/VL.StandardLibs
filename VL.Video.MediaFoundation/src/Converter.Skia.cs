using SharpDX.DXGI;
using SkiaSharp;
using System;
using VL.Core;
using VL.Skia;
using VL.Skia.Egl;

namespace VL.Video.MediaFoundation
{
    public sealed class SkiaConverter : Converter<SKImage>
    {
        private readonly RenderContext renderContext;
        private readonly IDisposable graphicsDeviceHandle;

        public SkiaConverter(bool useStrideDevice)
        {
            if (useStrideDevice)
            {
                (renderContext, graphicsDeviceHandle) = RenderContextProvider.GetStrideRenderContext();
            }
            else
            {
                renderContext = RenderContextProvider.GetSkiaRenderContext();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            renderContext.Dispose();
            graphicsDeviceHandle?.Dispose();
        }

        protected override SKImage Convert(VideoFrame frame)
        {
            const int GL_TEXTURE_BINDING_2D = 0x8069;

            renderContext.MakeCurrent();

            var eglContext = renderContext.EglContext;
            var eglImage = eglContext.CreateImageFromD3D11Texture(frame.NativeTexture.NativePointer);

            uint textureId = 0;
            NativeGles.glGenTextures(1, ref textureId);

            // We need to restore the currently bound texture (https://github.com/devvvvs/vvvv/issues/5925)
            NativeGles.glGetIntegerv(GL_TEXTURE_BINDING_2D, out var currentTextureId);
            NativeGles.glBindTexture(NativeGles.GL_TEXTURE_2D, textureId);
            NativeGles.glEGLImageTargetTexture2DOES(NativeGles.GL_TEXTURE_2D, eglImage);
            NativeGles.glBindTexture(NativeGles.GL_TEXTURE_2D, (uint)currentTextureId);

            var description = frame.NativeTexture.Description;
            var colorType = GetColorType(description.Format);
            var glInfo = new GRGlTextureInfo(
                id: textureId,
                target: NativeGles.GL_TEXTURE_2D,
                format: colorType.ToGlSizedFormat());

            var backendTexture = new GRBackendTexture(
                width: description.Width,
                height: description.Height,
                mipmapped: false,
                glInfo: glInfo);

            var image = SKImage.FromTexture(
                renderContext.SkiaContext,
                backendTexture,
                GRSurfaceOrigin.TopLeft,
                colorType,
                SKAlphaType.Premul,
                colorspace: SKColorSpace.CreateSrgb(),
                releaseProc: _ =>
                {
                    try
                    {
                        renderContext.MakeCurrent();

                        backendTexture.Dispose();
                        NativeGles.glDeleteTextures(1, ref textureId);
                        eglImage.Dispose();
                        frame.Dispose();
                    }
                    finally
                    {
                        renderContext.Release();
                    }
                });

            if (image != null)
            {
                frame.AddRef();
                renderContext.AddRef();
            }

            return image;
        }

        static SKColorType GetColorType(Format format)
        {
            switch (format)
            {
                case Format.B5G6R5_UNorm:
                    return SKColorType.Rgb565;
                case Format.B8G8R8A8_UNorm:
                case Format.B8G8R8A8_UNorm_SRgb:
                    return SKColorType.Bgra8888;
                case Format.R8G8B8A8_UNorm:
                case Format.R8G8B8A8_UNorm_SRgb:
                    return SKColorType.Rgba8888;
                case Format.R10G10B10A2_UNorm:
                    return SKColorType.Rgba1010102;
                case Format.R16G16B16A16_Float:
                    return SKColorType.RgbaF16;
                case Format.R16G16B16A16_UNorm:
                    return SKColorType.Rgba16161616;
                case Format.R32G32B32A32_Float:
                    return SKColorType.RgbaF32;
                case Format.R16G16_Float:
                    return SKColorType.RgF16;
                case Format.R16G16_UNorm:
                    return SKColorType.Rg1616;
                case Format.R8G8_UNorm:
                    return SKColorType.Rg88;
                case Format.A8_UNorm:
                    return SKColorType.Alpha8;
                case Format.R8_UNorm:
                    return SKColorType.Gray8;
                default:
                    return SKColorType.Unknown;
            }
        }
    }
}

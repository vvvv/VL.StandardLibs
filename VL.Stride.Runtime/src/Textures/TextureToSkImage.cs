#nullable enable
using SkiaSharp;
using Stride.Core;
using Stride.Graphics;
using VL.Core;
using VL.Skia;
using VL.Skia.Egl;

namespace VL.Stride.Textures
{
    /// <summary>
    /// Creates an image from the given texture. Note that the tooltip does not update correctly in case the texture mutates.
    /// </summary>
    /// <remarks>
    /// In case the texture is in sRGB format and the current color space is linear, a non-sRGB copy of the texture will be created internally.
    /// </remarks>
    [ProcessNode]
    public class TextureToSkImage
    {
        private static readonly PropertyKey<SKImage?> SKImageView = new (nameof(SKImageView), typeof(TextureToSkImage));
        private readonly RenderContextProvider renderContextProvider = AppHost.Current.GetRenderContextProvider();

        public SKImage? Update(Texture? texture)
        {
            if (texture is null)
                return null;

            var device = texture.GraphicsDevice;
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            {
                var nativeTexture = SharpDXInterop.GetNativeResource(texture) as SharpDX.Direct3D11.Texture2D;
                if (nativeTexture is null)
                    return null;

                var image = texture.Tags.Get(SKImageView);
                if (image is null)
                {
                    var renderContext = renderContextProvider.GetRenderContext();
                    image = D3D11Utils.TextureToSKImage(renderContext, nativeTexture.NativePointer, texture.ViewFormat.ToDXGIFormat()).DisposeBy(texture);
                    texture.Tags.Set(SKImageView, image);
                }
                return image;
            }

            return null;
        }
    }
}

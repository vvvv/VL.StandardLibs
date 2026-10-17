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
    [ProcessNode]
    public class TextureToSkImage
    {
        private static readonly PropertyKey<SKImage?> SKImageView = new (nameof(SKImageView), typeof(TextureToSkImage));
        private readonly RenderContextProvider renderContextProvider = AppHost.Current.GetRenderContextProvider();

        public unsafe SKImage? Update(Texture? texture)
        {
            if (texture is null)
                return null;

            var device = texture.GraphicsDevice;
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            {
                var nativeTexture = GraphicsMarshal.GetNativeResource(texture);
                if (nativeTexture.Handle is null)
                    return null;

                var image = texture.Tags.Get(SKImageView);
                if (image is null)
                {
                    var renderContext = renderContextProvider.GetRenderContext();
                    var useLinearColorspace = device.ColorSpace == ColorSpace.Linear && texture.Format == texture.ViewFormat && (texture.ViewFormat.IsHDR || texture.ViewFormat.IsSRgb);
                    image = D3D11Utils.TextureToSKImage(renderContext, (nint)nativeTexture.Handle, texture.ViewFormat.ToDXGIFormat(), useLinearColorspace: useLinearColorspace).DisposeBy(texture);
                    texture.Tags.Set(SKImageView, image);
                }
                return image;
            }

            return null;
        }
    }
}

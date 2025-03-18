using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using Stride.Core;
using Stride.Engine;
using Stride.Graphics;
using VL.Core;
using VL.Skia.Egl;
using SkiaRenderContext = VL.Skia.RenderContext;
using StrideRenderContext = Stride.Rendering.RenderContext;

namespace VL.Stride.Textures
{
    [ProcessNode]
    public class TextureToSkImage
    {
        private static readonly PropertyKey<SKImage> SKImageView = new (nameof(SKImageView), typeof(TextureToSkImage));
        private static readonly PropertyKey<Texture> NonSrgbTexture = new (nameof(NonSrgbTexture), typeof(TextureToSkImage));
        private readonly SkiaRenderContext renderContext = SkiaRenderContext.ForCurrentApp();
        private readonly CommandList commandList = GetCommandList();

        static CommandList GetCommandList()
        {
            return StrideRenderContext.GetShared(AppHost.Current.Services.GetRequiredService<Game>().Services).GetThreadContext().CommandList;
        }

        public SKImage? Update(Texture? texture)
        {
            if (texture is null)
                return null;

            // Maybe Skia update will help here - for now make the copy :(
            if (renderContext.UseLinearColorspace && texture.Format.IsSRgb())
            {
                var nonSrgbTexture = texture.Tags.Get(NonSrgbTexture);
                if (nonSrgbTexture is null)
                {
                    var desc = texture.Description;
                    desc.Format = desc.Format.ToNonSRgb();
                    desc.Options = TextureOptions.None;
                    nonSrgbTexture = Texture.New(texture.GraphicsDevice, desc).DisposeBy(texture);
                    texture.Tags.Set(NonSrgbTexture, nonSrgbTexture);
                }
                commandList.Copy(texture, nonSrgbTexture);
                return Update(nonSrgbTexture);
            }

            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            {
                var nativeTexture = SharpDXInterop.GetNativeResource(texture) as SharpDX.Direct3D11.Texture2D;
                if (nativeTexture is null)
                    return null;

                var image = texture.Tags.Get(SKImageView);
                if (image is null)
                {
                    image = D3D11Utils.TextureToSKImage(renderContext, nativeTexture.NativePointer).DisposeBy(texture);
                    texture.Tags.Set(SKImageView, image);
                }
                return image;
            }

            return null;
        }
    }
}

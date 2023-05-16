using Stride.Core;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using System;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Stride;

namespace VL.Video.MediaFoundation
{
    public sealed class StrideConverter : Converter<Texture>
    {
        private readonly IResourceHandle<RenderDrawContext> renderDrawContextHandle;
        private readonly RenderDrawContext renderDrawContext;
        private readonly GraphicsDevice graphicsDevice;

        public StrideConverter()
        {
            renderDrawContextHandle = AppHost.Current.Services.GetGameProvider()
                .Bind(g => RenderContext.GetShared(g.Services).GetThreadContext())
                .GetHandle() ?? throw new ServiceNotFoundException(typeof(IResourceProvider<Game>));

            renderDrawContext = renderDrawContextHandle.Resource;
            graphicsDevice = renderDrawContext.GraphicsDevice;
        }

        public override void Dispose()
        {
            base.Dispose();
            renderDrawContextHandle.Dispose();
        }

        protected override Texture Convert(VideoFrame frame)
        {
            var texture = SharpDXInterop.CreateTextureFromNative(graphicsDevice, frame.NativeTexture, takeOwnership: true);
            if (texture != null)
            {
                frame.AddRef();
                frame.DisposeBy(texture);
            }
            return ToDeviceColorSpace(texture);
        }

        private Texture ToDeviceColorSpace(Texture texture)
        {
            // Check if conversion is needed
            if (graphicsDevice.ColorSpace == ColorSpace.Gamma)
                return texture;

            var desc = texture.Description;
            if (desc.Format.IsSRgb() || !desc.Format.HasSRgbEquivalent())
                return texture;

            // Create texture with sRGB format
            desc.Format = desc.Format.ToSRgb();
            desc.Flags = TextureFlags.ShaderResource;
            var srgbTexture = Texture.New(graphicsDevice, desc);

            // Copy the texture
            renderDrawContext.CommandList.Copy(texture, srgbTexture);

            // Release input texture
            texture.Dispose();

            return srgbTexture;
        }
    }
}

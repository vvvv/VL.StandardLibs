using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using VL.Lib.Basics.Resources;
using Windows.Win32.Graphics.Direct3D11;

namespace VL.Skia.Video
{
    /// <summary>Staging textures of one description on one Direct3D 11 device, rented and returned per frame.</summary>
    [SupportedOSPlatform("windows6.1")]
    sealed unsafe class D3D11TexturePool : IDisposable
    {
        public static IResourceProvider<D3D11TexturePool> Get(ID3D11Device* graphicsDevice, in D3D11_TEXTURE2D_DESC description)
        {
            return ResourceProvider.NewPooledPerApp(((nint)graphicsDevice, description), x => new D3D11TexturePool((ID3D11Device*)x.Item1, x.description));
        }

        private readonly Stack<nint> textures = new Stack<nint>();
        private readonly ID3D11Device* graphicsDevice;
        private readonly D3D11_TEXTURE2D_DESC description;
        private bool isDisposed;

        public D3D11TexturePool(ID3D11Device* graphicsDevice, D3D11_TEXTURE2D_DESC description)
        {
            this.graphicsDevice = graphicsDevice;
            this.description = description;
        }

        public void Dispose()
        {
            isDisposed = true;

            lock (textures)
            {
                while (textures.Count > 0)
                    ((ID3D11Texture2D*)textures.Pop())->Release();
            }
        }

        public ID3D11Texture2D* Rent()
        {
            lock (textures)
            {
                if (textures.Count > 0)
                    return (ID3D11Texture2D*)textures.Pop();
            }

            ID3D11Texture2D* texture;
            var description = this.description;
            graphicsDevice->CreateTexture2D(&description, null, &texture);
            return texture;
        }

        public void Return(ID3D11Texture2D* texture)
        {
            if (isDisposed)
            {
                texture->Release();
                return;
            }

            lock (textures)
            {
                textures.Push((nint)texture);
            }
        }
    }
}

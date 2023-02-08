using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using VL.Lib.Basics.Resources;

namespace VL.Skia.Video
{
    sealed class D3D11TexturePool : IDisposable
    {
        public static IResourceProvider<D3D11TexturePool> Get(Device graphicsDevice, in Texture2DDescription description)
        {
            return ResourceProvider.NewPooledPerApp((graphicsDevice, description), x => new D3D11TexturePool(x.graphicsDevice, x.description));
        }

        private readonly Stack<Texture2D> textures = new Stack<Texture2D>();
        private readonly Device graphicsDevice;
        private readonly Texture2DDescription description;
        private bool isDisposed;

        public D3D11TexturePool(Device graphicsDevice, Texture2DDescription description)
        {
            this.graphicsDevice = graphicsDevice;
            this.description = description;
        }

        public ref readonly Texture2DDescription Description => ref description;

        public void Dispose()
        {
            isDisposed = true;

            while (textures.Count > 0)
                textures.Pop().Dispose();
        }

        public Texture2D Rent()
        {
            lock (textures)
            {
                if (textures.Count > 0)
                    return textures.Pop();
            }

            return new Texture2D(graphicsDevice, description);
        }

        public void Return(Texture2D texture)
        {
            if (isDisposed || texture.Description.Width != description.Width || texture.Description.Height != description.Height)
            {
                texture.Dispose();
                return;
            }

            lock (textures)
            {
                textures.Push(texture);
            }
        }
    }
}

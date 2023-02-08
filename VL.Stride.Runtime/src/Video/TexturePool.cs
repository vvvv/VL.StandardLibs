using Stride.Graphics;
using System;
using System.Collections.Generic;
using VL.Lib.Basics.Resources;

namespace VL.Stride.Video
{
    sealed class TexturePool : IDisposable
    {
        public static IResourceProvider<TexturePool> Get(GraphicsDevice graphicsDevice, TextureDescription description)
        {
            return ResourceProvider.NewPooledPerApp((graphicsDevice, description), x => new TexturePool(x.graphicsDevice, x.description));
        }

        private readonly Stack<Texture> textures = new Stack<Texture>();
        private readonly GraphicsDevice graphicsDevice;
        private readonly TextureDescription description;
        private bool isDisposed;

        public TexturePool(GraphicsDevice graphicsDevice, TextureDescription description)
        {
            this.graphicsDevice = graphicsDevice;
            this.description = description;
        }

        public ref readonly TextureDescription Description => ref description;

        public void Dispose()
        {
            isDisposed = true;

            while (textures.Count > 0)
                textures.Pop().Dispose();
        }

        public Texture Rent()
        {
            lock (textures)
            {
                if (textures.Count > 0)
                    return textures.Pop();
            }

            return Texture.New(graphicsDevice, description);
        }

        public void Return(Texture texture)
        {
            if (isDisposed || texture.Description != description)
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

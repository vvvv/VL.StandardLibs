using Stride.Core;
using Stride.Graphics;
using VL.Lib.Basics.Resources;

namespace VL.Video.MediaFoundation
{
    sealed class TextureRefCounter : IRefCounter<Texture>
    {
        public static readonly TextureRefCounter Default = new TextureRefCounter();

        private static readonly PropertyKey<bool> RefCountUnlocked = new PropertyKey<bool>(nameof(RefCountUnlocked), typeof(TextureRefCounter));

        public void Init(Texture resource)
        {
            if (resource is null)
                return;

            resource.Tags.Set(RefCountUnlocked, true);
        }

        public void AddRef(Texture resource)
        {
            if (resource is null)
                return;

            if (resource.Tags.ContainsKey(RefCountUnlocked))
            {
                IReferencable r = resource;
                r.AddReference();
            }
        }

        public void Release(Texture resource)
        {
            if (resource is null)
                return;

            if (resource.Tags.ContainsKey(RefCountUnlocked))
            {
                IReferencable r = resource;
                r.Release();
            }
        }
    }
}

using SharpDX.Direct3D11;
using System;
using System.Threading;

namespace VL.Video.MediaFoundation
{
    /// <summary>
    /// Used to manage the lifetime of the native texture through ref counting.
    /// When the ref count goes to zero the internal texture usually goes back into a pool.
    /// </summary>
    public sealed class VideoFrame : IDisposable
    {
        internal readonly Texture2D NativeTexture;

        private readonly IDisposable Handle;
        private int RefCount;

        internal VideoFrame(Texture2D nativeTexture, IDisposable handle)
        {
            NativeTexture = nativeTexture;
            Handle = handle;
            RefCount = 1;
        }

        internal void AddRef()
        {
            Interlocked.Increment(ref RefCount);
        }

        internal void Release()
        {
            if (Interlocked.Decrement(ref RefCount) == 0)
                Destroy();
        }

        public void Dispose()
        {
            Release();
        }

        void Destroy()
        {
            Handle.Dispose();
        }
    }
}

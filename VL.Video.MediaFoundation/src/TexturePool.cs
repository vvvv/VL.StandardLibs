using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace VL.Video.MediaFoundation
{
    /// <summary>
    /// Texture pooling mechanism used by the video player. Each texture gets wrapped in a video frame which allows to keep the texture alive through ref counting.
    /// </summary>
    sealed class TexturePool : IDisposable
    {
        private readonly Dictionary<Texture2DDescription, Stack<Texture2D>> cache = new Dictionary<Texture2DDescription, Stack<Texture2D>>();

        public TexturePool(Device device)
        {
            Device = device;
        }

        public Device Device { get; }

        internal VideoFrame Rent(in Texture2DDescription description)
        {
            lock (cache)
            {
                var stack = GetStack(in description);
                var texture = stack.Count > 0 ? stack.Pop() : new Texture2D(Device, description);
                return new VideoFrame(texture, Disposable.Create(texture, t => Return(t)));
            }
        }

        void Return(Texture2D texture)
        {
            lock (cache)
            {
                var description = texture.Description;
                var stack = GetStack(in description);
                stack.Push(texture);
            }
        }

        public void Recycle()
        {
            lock (cache)
            {
                foreach (var s in cache.Values)
                {
                    foreach (var t in s)
                        t.Dispose();
                }
                cache.Clear();
            }
        }

        public void Dispose()
        {
            Recycle();
        }

        private Stack<Texture2D> GetStack(in Texture2DDescription description)
        {
            return cache.EnsureValue(description, s => new Stack<Texture2D>(2));
        }
    }
}

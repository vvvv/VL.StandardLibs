#nullable enable
using System;
using System.Collections.Generic;
using VL.Lib.Basics.Imaging;

namespace VL.Lib.Basics.Video
{
    public sealed class VideoTexture : IDisposable
    {
        private readonly List<IDisposable> attachedResources = new();

        public VideoTexture(IntPtr nativePointer, int width, int height, PixelFormat pixelFormat)
        {
            NativePointer = nativePointer;
            Width = width;
            Height = height;
            PixelFormat = pixelFormat;
        }

        public IntPtr NativePointer { get; private set; }

        public int Width { get; }

        public int Height { get; }

        public PixelFormat PixelFormat { get; }

        public bool IsDisposed => NativePointer == IntPtr.Zero;

        public T? Get<T>() where T : class, IDisposable
        {
            ThrowIfDisposed();

            foreach (var r in attachedResources)
                if (r is T t)
                    return t;

            return default;
        }

        public T Attach<T>(T resource) where T : class, IDisposable
        {
            ThrowIfDisposed();

            attachedResources.Add(resource);
            return resource;
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            foreach (var r in attachedResources)
                r.Dispose();

            attachedResources.Clear();;

            NativePointer = default;
        }

        private void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }
    }
}

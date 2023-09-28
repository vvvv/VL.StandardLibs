#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;
using Windows.Win32.Graphics.Imaging;

namespace VL.Video.MF
{

    [SupportedOSPlatform("windows5.1.2600")]
    internal sealed unsafe class BitmapPool : IDisposable
    {
        private static readonly Dictionary<(uint width, uint height, Guid format), BitmapPool> pools = new();

        public static BitmapPool Get(IWICImagingFactory* factory, uint width, uint height, Guid format)
        {
            lock (pools)
            {
                var key = (width, height, format);
                if (!pools.TryGetValue(key, out var pool))
                {
                    pool = new BitmapPool(factory, width, height, format);
                    pools.Add(key, pool);
                }
                Interlocked.Increment(ref pool.refCount);
                return pool;
            }
        }

        private readonly Stack<IntPtr> pool = new();
        private readonly IWICImagingFactory* _factory;
        private readonly uint _width;
        private readonly uint _height;
        private readonly Guid _format;
        private bool isDisposed;
        private int refCount;

        private BitmapPool(IWICImagingFactory* factory, uint width, uint height, Guid format)
        {
            _factory = factory;
            _width = width;
            _height = height;
            _format = format;
            _factory->AddRef();
        }

        public IWICBitmap* Rent()
        {
            lock (pool)
            {
                if (pool.Count > 0)
                    return (IWICBitmap*)pool.Pop();

                IWICBitmap* bitmap;
                _factory->CreateBitmap(_width, _height, in _format, WICBitmapCreateCacheOption.WICBitmapCacheOnLoad, &bitmap);
                return bitmap;
            }
        }

        public void Return(IWICBitmap* bitmap)
        {
            lock (pool)
            {
                if (isDisposed || pool.Count > refCount * 2)
                {
                    bitmap->Release();
                }
                else
                {

                    pool.Push(new IntPtr(bitmap));

                }
            }
        }

        public void Dispose()
        {
            var refCount = Interlocked.Decrement(ref this.refCount);
            if (refCount == 0)
            {
                isDisposed = true;
                Recycle();

                lock (pools)
                {
                    pools.Remove((_width, _height, _format));
                }

                _factory->Release();
            }
        }

        private void Recycle()
        {
            lock (pool)
            {
                foreach (var t in pool)
                    ((IWICBitmap*)t)->Release();
                pool.Clear();
            }
        }
    }
}

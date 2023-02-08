using System;
using System.Diagnostics;
using System.Threading;

namespace VL.Skia
{
    public abstract class RefCounted : IDisposable
    {
        private int refCount = 1;

        public void AddRef() => Interlocked.Increment(ref refCount);

        public void Release()
        {
            var count = Interlocked.Decrement(ref refCount);
            Debug.Assert(count >= 0);
            if (count == 0)
                Destroy();
        }

        public void Dispose()
        {
            Release();
        }

        protected abstract void Destroy();
    }
}

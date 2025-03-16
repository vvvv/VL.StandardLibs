using System;
using System.Threading;

namespace VL.Skia.Egl
{
    public abstract class EglResource : IDisposable
    {
        private IntPtr nativePointer;

        public EglResource(IntPtr nativePointer)
        {
            if (nativePointer == default)
                throw new ArgumentNullException(nameof(nativePointer));

            this.nativePointer = nativePointer;
        }

        public IntPtr NativePointer => nativePointer != default ? nativePointer : throw new ObjectDisposedException(GetType().Name);

        public void Dispose()
        {
            var ptr = Interlocked.Exchange(ref nativePointer, IntPtr.Zero);
            if (ptr != IntPtr.Zero)
                Destroy(ptr);
        }

        protected abstract void Destroy(nint nativePointer);

        public static implicit operator IntPtr(EglResource resource)
        {
            if (resource is null)
                return default;
            return resource.NativePointer;
        }
    }
}

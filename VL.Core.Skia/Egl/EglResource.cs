using System;

namespace VL.Skia.Egl
{
    public abstract class EglResource : RefCounted
    {
        private IntPtr nativePointer;

        public EglResource(IntPtr nativePointer)
        {
            if (nativePointer == default)
                throw new ArgumentNullException(nameof(nativePointer));

            this.nativePointer = nativePointer;
        }

        public IntPtr NativePointer => nativePointer;

        public static implicit operator IntPtr(EglResource resource)
        {
            if (resource is null)
                return default;
            return resource.nativePointer;
        }
    }
}

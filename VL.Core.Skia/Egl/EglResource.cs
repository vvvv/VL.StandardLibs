using System;
using System.Runtime.InteropServices;

namespace VL.Skia
{
    // Needed for ABI compatibility
    public abstract class RefCounted : SafeHandle
    {
        protected RefCounted(nint invalidHandleValue, bool ownsHandle) : base(invalidHandleValue, ownsHandle)
        {
        }

        public new void Dispose()
        {
            base.Dispose();
        }
    }
}

namespace VL.Skia.Egl
{
    public abstract class EglResource : RefCounted
    {
        internal EglResource()
            : base(default, true)
        {
        }

        public EglResource(IntPtr nativePointer)
            : base(default, true)
        {
            if (nativePointer == default)
                throw new ArgumentNullException(nameof(nativePointer));

            SetHandle(nativePointer);
        }

        public override bool IsInvalid => handle == default;

        public IntPtr NativePointer
        {
            get
            {
                ObjectDisposedException.ThrowIf(IsClosed, this);
                return handle;
            }
        }

        public static implicit operator IntPtr(EglResource resource)
        {
            if (resource is null)
                return default;
            return resource.NativePointer;
        }
    }
}

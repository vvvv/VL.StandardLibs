using System;
using System.Runtime.InteropServices;

namespace VL.Skia.Egl
{
    public abstract class EglResource : SafeHandle
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

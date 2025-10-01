using System;
using static VL.Skia.Egl.NativeEgl;

namespace VL.Skia.Egl
{
    public sealed class EglImage : EglResource
    {
        private readonly EglDisplay display;

        public EglImage(EglDisplay display, IntPtr nativePointer) : base(nativePointer)
        {
            this.display = display;
        }

        protected override bool ReleaseHandle()
        {
            return eglDestroyImageKHR(display, handle);
        }
    }
}

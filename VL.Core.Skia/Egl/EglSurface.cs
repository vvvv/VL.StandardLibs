using System;
using Stride.Core.Mathematics;
using static VL.Skia.Egl.NativeEgl;

namespace VL.Skia.Egl
{
    public sealed class EglSurface : EglResource
    {
        private readonly EglDisplay display;

        public EglSurface(EglDisplay display, IntPtr nativePointer) : base(nativePointer)
        {
            this.display = display;

            bool success = false;
            display.DangerousAddRef(ref success);
        }

        public Int2 Size
        {
            get
            {
                Int2 size;
                eglQuerySurface(display.NativePointer, NativePointer, EGL_WIDTH, out size.X);
                eglQuerySurface(display.NativePointer, NativePointer, EGL_HEIGHT, out size.Y);
                return size;
            }
        }

        protected override bool ReleaseHandle()
        {
            try
            {
                return eglDestroySurface(display, handle);
            }
            finally
            {
                display.DangerousRelease();
            }
        }
    }
}

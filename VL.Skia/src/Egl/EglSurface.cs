using System;
using Stride.Core.Mathematics;

namespace VL.Skia.Egl
{
    public sealed class EglSurface : EglResource
    {
        private readonly EglDisplay display;

        public EglSurface(EglDisplay display, IntPtr nativePointer) : base(nativePointer)
        {
            this.display = display;
        }

        public Int2 Size
        {
            get
            {
                Int2 size;
                NativeEgl.eglQuerySurface(display, NativePointer, NativeEgl.EGL_WIDTH, out size.X);
                NativeEgl.eglQuerySurface(display, NativePointer, NativeEgl.EGL_HEIGHT, out size.Y);
                return size;
            }
        }

        protected override void Destroy()
        {
            NativeEgl.eglDestroySurface(display, NativePointer);
        }
    }
}

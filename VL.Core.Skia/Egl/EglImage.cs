using System;
using Stride.Core.Mathematics;

namespace VL.Skia.Egl
{
    public sealed class EglImage : EglResource
    {
        private readonly EglDisplay display;

        public EglImage(EglDisplay display, IntPtr nativePointer) : base(nativePointer)
        {
            this.display = display;
        }

        protected override void Destroy()
        {
            NativeEgl.eglDestroyImageKHR(display, NativePointer);
        }
    }
}

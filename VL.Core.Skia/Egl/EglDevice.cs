using System;
using EGLDeviceEXT = System.IntPtr;

namespace VL.Skia.Egl
{
    public sealed class EglDevice : EglResource
    {
        public static EglDevice FromD3D11(IntPtr d3dDevice)
        {
            var angleDevice = NativeEgl.eglCreateDeviceANGLE(NativeEgl.EGL_D3D11_DEVICE_ANGLE, d3dDevice, null);
            if (angleDevice == default)
                throw new Exception("Failed to create EGL device");
            return new EglDevice(angleDevice);
        }

        private EglDevice(EGLDeviceEXT angleDevice)
            : base(angleDevice)
        {
        }

        protected override void Destroy()
        {
            NativeEgl.eglReleaseDeviceANGLE(NativePointer);
        }
    }
}

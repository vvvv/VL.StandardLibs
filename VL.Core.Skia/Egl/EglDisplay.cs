#nullable enable
using System;
using System.Collections.Generic;

using EGLDisplay = System.IntPtr;

namespace VL.Skia.Egl
{
    public sealed class EglDisplay : EglResource
    {
        public static EglDisplay? FromDevice(EglDevice angleDevice)
        {
            var display = NativeEgl.eglGetPlatformDisplayEXT(NativeEgl.EGL_PLATFORM_DEVICE_EXT, angleDevice, null);
            if (display == default)
                throw new Exception("Failed to get EGL display from device");
            if (NativeEgl.eglInitialize(display, out int major, out int minor) == NativeEgl.EGL_FALSE)
                throw new Exception("Failed to initialize EGL display from device");
            return new EglDisplay(display);
        }

        private EglDisplay(EGLDisplay nativePointer) : base(nativePointer)
        {
        }

        public bool TryGetD3D11Device(out IntPtr d3dDevice)
        {
            if (NativeEgl.eglQueryDisplayAttribEXT(NativePointer, NativeEgl.EGL_DEVICE_EXT, out var devicePtr) == NativeEgl.EGL_TRUE &&
                NativeEgl.eglQueryDeviceAttribEXT(devicePtr, NativeEgl.EGL_D3D11_DEVICE_ANGLE, out d3dDevice) == NativeEgl.EGL_TRUE)
            {
                return true;
            }
            else
            {
                d3dDevice = default;
                return false;
            }
        }

        public void BindTexImage(EglSurface surface)
        {
            if (NativeEgl.eglBindTexImage(NativePointer, surface, NativeEgl.EGL_BACK_BUFFER) == NativeEgl.EGL_FALSE)
                throw new Exception("Failed to bind texture image");
        }

        public void ReleaseTexImage(EglSurface surface)
        {
            if (NativeEgl.eglReleaseTexImage(NativePointer, surface, NativeEgl.EGL_BACK_BUFFER) == NativeEgl.EGL_FALSE)
                throw new Exception("Failed to release texture image");
        }

        protected override void Destroy(nint nativePointer)
        {
            NativeEgl.eglTerminate(nativePointer);
        }
    }
}

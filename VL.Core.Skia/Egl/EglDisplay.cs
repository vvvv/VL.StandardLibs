#nullable enable
using System;

using EGLDisplay = System.IntPtr;
using static VL.Skia.Egl.NativeEgl;
using VL.Core;

namespace VL.Skia.Egl
{
    public sealed class EglDisplay : EglResource
    {
        public static EglDisplay ForCurrentApp()
        {
            var appHost = AppHost.CurrentOrGlobal;
            return appHost.Services.GetOrAddService(s =>
            {
                var device = EglDevice.ForCurrentApp();
                return FromDevice(device);
            }, allowToAskParent: false);
        }

        public static EglDisplay FromDevice(EglDevice angleDevice)
        {
            var display = eglGetPlatformDisplayEXT(EGL_PLATFORM_DEVICE_EXT, angleDevice, null);
            if (display == default)
                throw new Exception("Failed to get EGL display from device");
            if (!eglInitialize(display, out int major, out int minor))
                throw new Exception("Failed to initialize EGL display from device");
            return new EglDisplay(angleDevice, display);
        }

        private readonly EglDevice device;

        private EglDisplay(EglDevice device, EGLDisplay nativePointer) : base(nativePointer)
        {
            this.device = device;

            var success = false;
            device.DangerousAddRef(ref success);
        }

        public bool UseLinearColorspace => device.UseLinearColorspace;

        public bool TryGetD3D11Device(out IntPtr d3dDevice)
        {
            if (eglQueryDisplayAttribEXT(NativePointer, EGL_DEVICE_EXT, out var devicePtr) &&
                eglQueryDeviceAttribEXT(devicePtr, EGL_D3D11_DEVICE_ANGLE, out d3dDevice))
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
            if (!eglBindTexImage(NativePointer, surface, EGL_BACK_BUFFER))
                throw new Exception("Failed to bind texture image");
        }

        public void ReleaseTexImage(EglSurface surface)
        {
            if (!eglReleaseTexImage(NativePointer, surface, EGL_BACK_BUFFER))
                throw new Exception("Failed to release texture image");
        }

        protected override bool ReleaseHandle()
        {
            try
            {
                return eglTerminate(handle);
            }
            finally
            {
                device.DangerousRelease();
            }
        }
    }
}

#nullable enable
using System;

using EGLDisplay = System.IntPtr;
using static VL.Skia.Egl.NativeEgl;
using Windows.Win32.Graphics.Direct3D11;

namespace VL.Skia.Egl
{
    sealed class EglDisplayProvider : IDisposable
    {
        private readonly EglDeviceProvider deviceProvider;
        private EglDisplay? display;

        public EglDisplayProvider(EglDeviceProvider deviceProvider)
        {
            this.deviceProvider = deviceProvider;
        }

        public EglDisplay GetDisplay()
        {
            var device = deviceProvider.GetDevice();
            if (device != display?.Device)
            {
                display?.Dispose();
                display = null;
            }
            return display ??= EglDisplay.FromDevice(device);
        }

        public void Dispose()
        {
            display?.Dispose();
            display = null;
        }
    }

    public sealed class EglDisplay : EglResource
    {
        public static EglDisplay FromDevice(EglDevice angleDevice)
        {
            var display = eglGetPlatformDisplayEXT(EGL_PLATFORM_DEVICE_EXT, angleDevice, null);
            if (display == default)
                throw new EglException("Failed to get EGL display from device");
            if (!eglInitialize(display, out int major, out int minor))
                throw new EglException("Failed to initialize EGL display from device");
            return new EglDisplay(angleDevice, display);
        }

        private readonly EglDevice device;

        private EglDisplay(EglDevice device, EGLDisplay nativePointer) : base(nativePointer)
        {
            this.device = device;

            var success = false;
            device.DangerousAddRef(ref success);
        }

        public EglDevice Device => device;

        public bool UseLinearColorspace => device.UseLinearColorspace;

        public bool IsLost => device.IsLost;

        public unsafe bool TryGetD3D11Device(out IntPtr d3dDevice)
        {
            d3dDevice = (nint)Device.D3D11Device;
            return d3dDevice != IntPtr.Zero;
        }

        internal unsafe bool TryGetD3D11DeviceInterface(out ID3D11Device* d3dDevice)
        {
            d3dDevice = Device.D3D11Device;
            return d3dDevice != null;
        }

        public void BindTexImage(EglSurface surface)
        {
            if (!eglBindTexImage(NativePointer, surface, EGL_BACK_BUFFER))
                throw new EglException("Failed to bind texture image");
        }

        public void ReleaseTexImage(EglSurface surface)
        {
            if (!eglReleaseTexImage(NativePointer, surface, EGL_BACK_BUFFER))
                throw new EglException("Failed to release texture image");
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

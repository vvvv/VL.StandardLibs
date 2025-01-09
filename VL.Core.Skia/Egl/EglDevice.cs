#nullable enable
using System;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D11;
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

        [SupportedOSPlatform("windows")]
        public static unsafe EglDevice NewD3D11()
        {
            D3D_FEATURE_LEVEL featureLevels = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1;
            D3D_FEATURE_LEVEL featureLevel = default;
            D3D11_CREATE_DEVICE_FLAG flags = default;

            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--debug-gpu"))
                flags |= D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_DEBUG;

            Windows.Win32.PInvoke.D3D11CreateDevice(null, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE, default, flags, &featureLevels, 1, 7, out var device, &featureLevel, out var context);
            if (device is null)
                Windows.Win32.PInvoke.D3D11CreateDevice(null, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE, default, flags, default, 0, 7, out device, &featureLevel, out context);
            if (device is null)
                throw new Exception("Failed to create D3D11 device");

            var angleDevice = NativeEgl.eglCreateDeviceANGLE(NativeEgl.EGL_D3D11_DEVICE_ANGLE, Marshal.GetIUnknownForObject(device), null);
            if (angleDevice == default)
                throw new Exception("Failed to create EGL device");

            return new EglDevice(angleDevice, Disposable.Create(() =>
            {
                Marshal.ReleaseComObject(context);
                Marshal.ReleaseComObject(device);
            }));
        }

        private IDisposable? cleanup;

        private EglDevice(EGLDeviceEXT angleDevice, IDisposable? cleanup = null)
            : base(angleDevice)
        {
            this.cleanup = cleanup;
        }

        protected override void Destroy()
        {
            NativeEgl.eglReleaseDeviceANGLE(NativePointer);
            cleanup?.Dispose();
        }
    }
}

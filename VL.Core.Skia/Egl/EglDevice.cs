#nullable enable
using System;
using System.Runtime.Versioning;
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

        [SupportedOSPlatform("windows6.1")]
        public static unsafe EglDevice NewD3D11()
        {
            D3D_FEATURE_LEVEL featureLevels = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1;
            D3D_FEATURE_LEVEL featureLevel = default;
            D3D11_CREATE_DEVICE_FLAG flags = default;

            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--debug-gpu"))
                flags |= D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_DEBUG;

            ID3D11Device* device;
            ID3D11DeviceContext* deviceContext;
            Windows.Win32.PInvoke.D3D11CreateDevice(null, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE, default, flags, &featureLevels, 1, 7, &device, &featureLevel, &deviceContext);
            if (device is null)
                Windows.Win32.PInvoke.D3D11CreateDevice(null, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE, default, flags, default, 0, 7, &device, &featureLevel, &deviceContext);
            if (device is null)
                throw new Exception("Failed to create D3D11 device");

            try
            {
                var angleDevice = NativeEgl.eglCreateDeviceANGLE(NativeEgl.EGL_D3D11_DEVICE_ANGLE, (nint)device, null);
                if (angleDevice == default)
                    throw new Exception("Failed to create EGL device");

                return new EglDevice(angleDevice, (nint)device);
            }
            finally
            {
                deviceContext->Release();
                device->Release();
            }
        }


        private EglDevice(EGLDeviceEXT angleDevice, nint? nativeDevice = default)
            : base(angleDevice)
        {
            //this.nativeDevice = nativeDevice;
        }

        protected override void Destroy()
        {
            NativeEgl.eglReleaseDeviceANGLE(NativePointer);
        }

        // Helpful To debug memory leaks
        /*
        public int InternalRefCount
        {
            get
            {
                if (nativeDevice.HasValue)
                {
                    Marshal.AddRef(nativeDevice.Value);
                    return Marshal.Release(nativeDevice.Value);
                }
                return -1;
            }
        }
        private nint? nativeDevice;
        */
    }
}

#nullable enable
using System;
using System.Runtime.Versioning;
using VL.Core.Skia;
using Windows.Win32;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D11;
using EGLDeviceEXT = System.IntPtr;

namespace VL.Skia.Egl
{
    public unsafe sealed class EglDevice : EglResource
    {
        public static EglDevice FromD3D11(IntPtr d3dDevice)
        {
            //using var device = D3D11Utils.GetD3D11Device1((ID3D11Device*)d3dDevice);
            //D3D_FEATURE_LEVEL chosenFeatureLevel;
            //ID3DDeviceContextState* deviceContextState;
            //device.Ptr->CreateDeviceContextState(
            //    0,
            //    [device.Ptr->GetFeatureLevel()],
            //    PInvoke.D3D11_SDK_VERSION,
            //    in ID3D11Device1.IID_Guid,
            //    &chosenFeatureLevel,
            //    &deviceContextState);

            //using var ctx = D3D11Utils.GetD3D11DeviceContext1((ID3D11Device*)d3dDevice);
            //ID3DDeviceContextState* p;
            //ctx.Ptr->SwapDeviceContextState(deviceContextState, &p);

            try
            {
                var angleDevice = NativeEgl.eglCreateDeviceANGLE(NativeEgl.EGL_D3D11_DEVICE_ANGLE, d3dDevice, null);
                if (angleDevice == default)
                    throw new Exception("Failed to create EGL device");
                return new EglDevice(angleDevice, isOwner: false, d3dDevice/*, (nint)deviceContextState*/);
            }
            finally
            {
                //ctx.Ptr->SwapDeviceContextState(p);
                //p->Release();
            }
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

                return new EglDevice(angleDevice, isOwner: true, (nint)device);
            }
            finally
            {
                deviceContext->Release();
                device->Release();
            }
        }


        private EglDevice(EGLDeviceEXT angleDevice, bool isOwner, nint? nativeDevice = default, nint contextState = default)
            : base(angleDevice)
        {
            IsOwner = isOwner;
            ContextState = contextState;
            //this.nativeDevice = nativeDevice;
        }

        public bool IsOwner { get; }

        public nint ContextState { get; }

        protected override void Destroy(nint nativePointer)
        {
            NativeEgl.eglReleaseDeviceANGLE(nativePointer);
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

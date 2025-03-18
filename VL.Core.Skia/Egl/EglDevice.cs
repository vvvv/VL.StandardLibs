#nullable enable
using System;
using System.Runtime.Versioning;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D11;
using EGLDeviceEXT = System.IntPtr;
using static VL.Skia.Egl.NativeEgl;
using VL.Core;
using VL.Lib.Basics.Video;
using Microsoft.Extensions.DependencyInjection;

namespace VL.Skia.Egl
{
    public unsafe sealed class EglDevice : EglResource
    {
        public static EglDevice ForCurrentApp()
        {
            var appHost = AppHost.CurrentOrGlobal;
            return appHost.Services.GetOrAddService(s =>
            {
                if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                {
                    EglDevice device;
                    // TODO: Hmm, because of extensions assemblies being part of service scope we always create a Stride game this way - ideas?
                    if (s.GetService<IGraphicsDeviceProvider>() is IGraphicsDeviceProvider graphicsDeviceProvider &&
                        graphicsDeviceProvider.Type == GraphicsDeviceType.Direct3D11)
                    {
                        device = FromD3D11(graphicsDeviceProvider.NativePointer, graphicsDeviceProvider);
                    }
                    else
                    {
                        device = NewD3D11();
                    }
                    return device;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }, allowToAskParent: false);
        }

        public static EglDevice FromD3D11(IntPtr d3dDevice, IGraphicsDeviceProvider? graphicsDeviceProvider = null)
        {
            var angleDevice = eglCreateDeviceANGLE(EGL_D3D11_DEVICE_ANGLE, d3dDevice, null);
            if (angleDevice == default)
                throw new Exception("Failed to create EGL device");
            return new EglDevice(angleDevice, d3dDevice, graphicsDeviceProvider: graphicsDeviceProvider);
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
                var angleDevice = eglCreateDeviceANGLE(EGL_D3D11_DEVICE_ANGLE, (nint)device, null);
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

        private readonly IGraphicsDeviceProvider? graphicsDeviceProvider;

        private EglDevice(EGLDeviceEXT angleDevice, nint? nativeDevice = default, nint contextState = default, IGraphicsDeviceProvider? graphicsDeviceProvider = null)
            : base(angleDevice)
        {
            ContextState = contextState;
            this.graphicsDeviceProvider = graphicsDeviceProvider;
            //this.nativeDevice = nativeDevice;
        }

        public nint ContextState { get; }

        public bool UseLinearColorspace => graphicsDeviceProvider?.UseLinearColorspace ?? false;

        protected override bool ReleaseHandle()
        {
            return eglReleaseDeviceANGLE(handle);
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

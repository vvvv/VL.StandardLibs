#nullable enable
using System;
using System.Runtime.Versioning;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D11;
using EGLDeviceEXT = System.IntPtr;
using static VL.Skia.Egl.NativeEgl;
using VL.Core;
using VL.Lib.Basics.Video;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace VL.Skia.Egl
{
    sealed class EglDeviceProvider : IDisposable
    {
        private readonly IGraphicsDeviceProvider? graphicsDeviceProvider;
        private readonly ILogger? logger;
        private EglDevice? device;

        public EglDeviceProvider(IGraphicsDeviceProvider? graphicsDeviceProvider = null, ILogger? logger = null)
        {
            this.graphicsDeviceProvider = graphicsDeviceProvider;
            this.logger = logger;
        }

        public EglDevice GetDevice()
        {
            if (device != null && device.IsLost)
            {
                device.Dispose();
                device = null;
            }

            return device ??= CreateDevice();
        }

        public void Dispose()
        {
            device?.Dispose();
            device = null;
        }

        private EglDevice CreateDevice()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            {
                EglDevice device;
                // TODO: Hmm, because of extensions assemblies being part of service scope we always create a Stride game this way - ideas?
                if (graphicsDeviceProvider?.Type == GraphicsDeviceType.Direct3D11)
                {
                    logger?.LogInformation("Using existing D3D11 device for ANGLE on thread {threadName}", GetThreadName());
                    device = EglDevice.FromD3D11(graphicsDeviceProvider.NativePointer, graphicsDeviceProvider.UseLinearColorspace);
                }
                else
                {
                    logger?.LogInformation("Creating new D3D11 device for ANGLE on thread {threadName}", GetThreadName());
                    device = EglDevice.NewD3D11();
                }
                return device;
            }
            else
            {
                throw new NotImplementedException();
            }

            static string GetThreadName() => Thread.CurrentThread.Name ?? Thread.CurrentThread.ManagedThreadId.ToString();
        }
    }

    public unsafe sealed class EglDevice : EglResource
    {
        public static EglDevice FromD3D11(IntPtr d3dDevice, bool useLinearColorSpace = false)
        {
            var angleDevice = eglCreateDeviceANGLE(EGL_D3D11_DEVICE_ANGLE, d3dDevice, null);
            if (angleDevice == default)
                throw new EglException("Failed to create EGL device");
            return new EglDevice(angleDevice, (ID3D11Device*)d3dDevice, useLinearColorSpace: useLinearColorSpace);
        }

        [SupportedOSPlatform("windows6.1")]
        public static unsafe EglDevice NewD3D11()
        {
            D3D_FEATURE_LEVEL featureLevels = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1;
            D3D_FEATURE_LEVEL featureLevel = default;
            D3D11_CREATE_DEVICE_FLAG flags = D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT;

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
                    throw new EglException("Failed to create EGL device");

                return new EglDevice(angleDevice, device);
            }
            finally
            {
                deviceContext->Release();
                device->Release();
            }
        }

        private readonly ID3D11Device* d3d11Device;

        private EglDevice(EGLDeviceEXT angleDevice, ID3D11Device* d3d11Device, nint contextState = default, bool useLinearColorSpace = false)
            : base(angleDevice)
        {
            ContextState = contextState;
            UseLinearColorspace = useLinearColorSpace;
            this.d3d11Device = d3d11Device;
        }

        public nint ContextState { get; }

        public bool UseLinearColorspace { get; }

        public bool IsLost
        {
            get
            {
                if (OperatingSystem.IsWindowsVersionAtLeast(6, 1) && !d3d11Device->GetDeviceRemovedReason().Succeeded)
                    return true;
                return false;
            }
        }

        internal ID3D11Device* D3D11Device => d3d11Device;

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
        */
    }
}

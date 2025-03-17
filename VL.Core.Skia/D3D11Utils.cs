using System;
using System.Runtime.Versioning;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.System.Com;
using static Windows.Win32.PInvoke;
using Windows.Win32.Graphics.Direct3D;

namespace VL.Core.Skia
{
    static unsafe class D3D11Utils
    {
        [SupportedOSPlatform("windows8.0")]
        public static ComPtr<ID3D11Device1> GetD3D11Device1(nint d3d11Device) => GetD3D11Device1((ID3D11Device*)d3d11Device);

        [SupportedOSPlatform("windows8.0")]
        public static ComPtr<ID3D11Device1> GetD3D11Device1(ID3D11Device* device)
        {
            device->QueryInterface(in ID3D11Device1.IID_Guid, out var device1).ThrowOnFailure();
            return (ID3D11Device1*)device1;
        }

        [SupportedOSPlatform("windows8.0")]
        public static ComPtr<ID3D11DeviceContext1> GetD3D11DeviceContext1(nint d3d11Device) => GetD3D11DeviceContext1((ID3D11Device*)d3d11Device);

        [SupportedOSPlatform("windows8.0")]
        public static ComPtr<ID3D11DeviceContext1> GetD3D11DeviceContext1(ID3D11Device* device)
        {
            ID3D11DeviceContext* deviceContext;
            device->GetImmediateContext(&deviceContext);

            try
            {
                deviceContext->QueryInterface(in ID3D11DeviceContext1.IID_Guid, out var deviceContext1).ThrowOnFailure();
                return (ID3D11DeviceContext1*)deviceContext1;
            }
            finally
            {
                deviceContext->Release();
            }
        }

        // Calls release on context once the scope is disposed
        [SupportedOSPlatform("windows8.0")]
        public static DeviceContextScope SwitchState(ID3D11DeviceContext1* context, ID3DDeviceContextState* state)
        {
            return new DeviceContextScope(context, state);
        }

        [SupportedOSPlatform("windows8.0")]
        public static ID3DDeviceContextState* CreateDeviceContextState(ID3D11Device1* device)
        {
            D3D_FEATURE_LEVEL chosenFeatureLevel;
            ID3DDeviceContextState* deviceContextState;
            device->CreateDeviceContextState(
                0,
                [device->GetFeatureLevel()],
                D3D11_SDK_VERSION,
                in ID3D11Device1.IID_Guid,
                &chosenFeatureLevel,
                &deviceContextState);
            return deviceContextState;
        }

        [SupportedOSPlatform("windows8.0")]
        public unsafe ref struct DeviceContextScope : IDisposable
        {
            readonly ID3D11DeviceContext1* deviceContext;
            readonly ID3DDeviceContextState* previousState;

            internal DeviceContextScope(ID3D11DeviceContext1* deviceContext, ID3DDeviceContextState* newState)
            {
                this.deviceContext = deviceContext;

                ID3DDeviceContextState* previousState;
                deviceContext->SwapDeviceContextState(newState, &previousState);
                this.previousState = previousState;
            }

            public void Dispose()
            {
                if (deviceContext is not null)
                {
                    deviceContext->OMSetRenderTargets(0);
                    deviceContext->SwapDeviceContextState(previousState);
                    previousState->Release();
                    deviceContext->Release();
                }
            }
        }
    }

#pragma warning disable CS8500
    internal unsafe readonly ref struct ComPtr<T>(T* ptr) : IDisposable
    {
        public T* Ptr => ptr;
        public bool IsDefault => ptr is null;
        public void Dispose() => ((IUnknown*)Ptr)->Release();

        public static implicit operator ComPtr<T>(T* ptr) => new ComPtr<T>(ptr);
        public static implicit operator T*(ComPtr<T> ptr) => ptr.Ptr;
    }
}

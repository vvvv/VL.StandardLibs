using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.System.Com;
using static Windows.Win32.PInvoke;
using static Windows.Win32.Graphics.Direct3D11.D3D11_1_CREATE_DEVICE_CONTEXT_STATE_FLAG;
using Windows.Win32.Graphics.Direct3D;

namespace VL.Core.Skia
{
    static unsafe class D3D11Utils
    {
        [SupportedOSPlatform("windows8.0")]
        public static ComPtr<ID3D11Device1> GetD3D11Device1(ID3D11Device* device)
        {
            device->QueryInterface(in ID3D11Device1.IID_Guid, out var device1).ThrowOnFailure();
            return (ID3D11Device1*)device1;
        }

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

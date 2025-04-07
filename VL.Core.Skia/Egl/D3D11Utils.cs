using SkiaSharp;
using System;
using System.Runtime.Versioning;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.System.Com;
using static Windows.Win32.PInvoke;
using static Windows.Win32.Graphics.Dxgi.Common.DXGI_FORMAT;
using System.Diagnostics;

namespace VL.Skia.Egl
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

        [SupportedOSPlatform("windows6.1")]
        public static unsafe SKImage SharedHandleToSKImage(RenderContext renderContext, nint sharedHandle)
        {
            if (!renderContext.EglContext.Display.TryGetD3D11Device(out var d3dDevicePtr))
                return null;

            var d3dDevice = (ID3D11Device*)d3dDevicePtr;
            // Open on D3D11 device of vvvv
            ID3D11Texture2D* d3d11Texture;
            {
                ID3D11Resource* sharedResource;
                Guid iid = ID3D11Resource.IID_Guid;
                d3dDevice->OpenSharedResource((Windows.Win32.Foundation.HANDLE)sharedHandle, &iid, (void**)&sharedResource);
                sharedResource->QueryInterface(out d3d11Texture);
                sharedResource->Release();
            }

            try
            {
                return TextureToSKImage(renderContext, (nint)d3d11Texture);
            }
            finally
            {
                d3d11Texture->Release();
            }
        }

        [SupportedOSPlatform("windows6.1")]
        public static unsafe SKImage TextureToSKImage(RenderContext renderContext, nint d3d11Texture)
        {
            using var _ = renderContext.MakeCurrent(forRendering: false);

            using var eglImage = renderContext.EglContext.CreateImageFromD3D11Texture(d3d11Texture);

            uint textureId = 0;
            NativeGles.glGenTextures(1, ref textureId);

            // We need to restore the currently bound texture (https://github.com/devvvvs/vvvv/issues/5925)
            NativeGles.glGetIntegerv(NativeGles.GL_TEXTURE_BINDING_2D, out var currentTextureId);
            NativeGles.glBindTexture(NativeGles.GL_TEXTURE_2D, textureId);
            NativeGles.glEGLImageTargetTexture2DOES(NativeGles.GL_TEXTURE_2D, eglImage);
            NativeGles.glBindTexture(NativeGles.GL_TEXTURE_2D, (uint)currentTextureId);

            ID3D11Texture2D* texture = (ID3D11Texture2D*)d3d11Texture;
            texture->GetDesc(out var desc);

            var colorType = desc.Format.ToSkColorType();
            var glInfo = new GRGlTextureInfo(
                id: textureId,
                target: NativeGles.GL_TEXTURE_2D,
                format: colorType.ToGlSizedFormat());

            using var backendTexture = new GRBackendTexture(
                width: (int)desc.Width,
                height: (int)desc.Height,
                mipmapped: false,
                glInfo: glInfo);

            return SKImage.FromTexture(
                renderContext.SkiaContext,
                backendTexture,
                GRSurfaceOrigin.TopLeft,
                colorType,
                SKAlphaType.Unpremul,
                // TODO: Check this, seems to make no difference
                colorspace: renderContext.UseLinearColorspace && desc.Format.ShouldUseLinearColorSpace() ? SKColorSpace.CreateSrgbLinear() : SKColorSpace.CreateSrgb(),
                releaseProc: x =>
                {
                    var (renderContext, textureId) = ((RenderContext, uint))x;
                    if (renderContext.IsOnCorrectThread && !renderContext.IsDisposed)
                    {
                        using (renderContext.MakeCurrent(forRendering: false))
                            NativeGles.glDeleteTextures(1, ref textureId);
                    }
                    else
                    {
                        Trace.TraceWarning($"Resource leak detected");
                    }
                },
                releaseContext: (renderContext, textureId));
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

        // Thanks AI for these...

        /// <summary>
        /// Converts a DXGI_FORMAT to its equivalent SKColorType
        /// </summary>
        /// <param name="format">The DXGI format to convert</param>
        /// <returns>The corresponding SKColorType or Unknown if no appropriate mapping exists</returns>
        public static SKColorType ToSkColorType(this DXGI_FORMAT format)
        {
            switch (format)
            {
                // Standard formats
                case DXGI_FORMAT_R8G8B8A8_UNORM:
                case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
                    return SKColorType.Rgba8888;

                case DXGI_FORMAT_B8G8R8A8_UNORM:
                case DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
                case DXGI_FORMAT_B8G8R8X8_UNORM:
                case DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
                    return SKColorType.Bgra8888;

                // High precision formats
                case DXGI_FORMAT_R16G16B16A16_FLOAT:
                    return SKColorType.RgbaF16;

                case DXGI_FORMAT_R32G32B32A32_FLOAT:
                    return SKColorType.RgbaF32;

                // Two component formats
                case DXGI_FORMAT_R8G8_UNORM:
                    return SKColorType.Rg88;

                case DXGI_FORMAT_R16G16_FLOAT:
                    return SKColorType.RgF16;

                case DXGI_FORMAT_R16G16_UNORM:
                    return SKColorType.Rg1616;

                // Single channel formats
                case DXGI_FORMAT_R8_UNORM:
                    return SKColorType.Gray8;

                case DXGI_FORMAT_A8_UNORM:
                    return SKColorType.Alpha8;

                // Special formats
                case DXGI_FORMAT_R10G10B10A2_UNORM:
                    return SKColorType.Rgba1010102;

                case DXGI_FORMAT_B5G6R5_UNORM:
                    return SKColorType.Rgb565;

                // Default case for unsupported formats
                default:
                    return SKColorType.Unknown;
            }
        }

        /// <summary>
        /// Determines whether the given DXGI format should use a linear color space
        /// </summary>
        /// <param name="format">The DXGI format to check</param>
        /// <returns>True if the format should use a linear color space, false otherwise</returns>
        public static bool ShouldUseLinearColorSpace(this DXGI_FORMAT format)
        {
            if (IsSrgbFormat(format))
                return true;

            if (IsHdrFormat(format))
                return true;

            // Default to false for standard formats
            return false;
        }

        /// <summary>
        /// Determines if the format is an sRGB format
        /// </summary>
        /// <param name="format">The DXGI format to check</param>
        /// <returns>True if the format is an sRGB format</returns>
        private static bool IsSrgbFormat(DXGI_FORMAT format)
        {
            return format switch
            {
                // Formats with explicit sRGB in their name
                DXGI_FORMAT_R8G8B8A8_UNORM_SRGB => true,
                DXGI_FORMAT_B8G8R8A8_UNORM_SRGB => true,
                DXGI_FORMAT_B8G8R8X8_UNORM_SRGB => true,
                DXGI_FORMAT_BC1_UNORM_SRGB => true,
                DXGI_FORMAT_BC2_UNORM_SRGB => true,
                DXGI_FORMAT_BC3_UNORM_SRGB => true,
                DXGI_FORMAT_BC7_UNORM_SRGB => true,
                _ => false
            };
        }

        /// <summary>
        /// Determines if the format is a high dynamic range (HDR) format
        /// </summary>
        /// <param name="format">The DXGI format to check</param>
        /// <returns>True if the format is an HDR format</returns>
        private static bool IsHdrFormat(DXGI_FORMAT format)
        {
            return format switch
            {
                // Floating point formats are typically linear
                DXGI_FORMAT_R16_FLOAT => true,
                DXGI_FORMAT_R16G16_FLOAT => true,
                DXGI_FORMAT_R16G16B16A16_FLOAT => true,
                DXGI_FORMAT_R32_FLOAT => true,
                DXGI_FORMAT_R32G32_FLOAT => true,
                DXGI_FORMAT_R32G32B32_FLOAT => true,
                DXGI_FORMAT_R32G32B32A32_FLOAT => true,

                // Some 10-bit formats can be considered HDR/linear
                DXGI_FORMAT_R10G10B10A2_UNORM => true,
                DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM => true,
                DXGI_FORMAT_R11G11B10_FLOAT => true,

                // Shared HDR formats
                DXGI_FORMAT_R9G9B9E5_SHAREDEXP => true,

                _ => false
            };
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

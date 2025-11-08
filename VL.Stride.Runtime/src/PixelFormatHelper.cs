using SkiaSharp;
using Stride.Graphics;
using System;
using Windows.Win32.Graphics.Dxgi.Common;

namespace VL.Stride;

/// <summary>
/// Helper class for converting between Stride PixelFormat, SKColorType, and DXGI_FORMAT.
/// </summary>
internal static class PixelFormatHelper
{
    /// <summary>
    /// Converts a Stride PixelFormat to the equivalent SKColorType.
    /// </summary>
    /// <param name="format">The Stride pixel format.</param>
    /// <returns>The equivalent SKColorType, or SKColorType.Unknown if no mapping exists.</returns>
    public static SKColorType ToSKColorType(PixelFormat format)
    {
        return format switch
        {
            // 8-bit per channel formats
            PixelFormat.B8G8R8A8_UNorm => SKColorType.Bgra8888,
            PixelFormat.B8G8R8A8_UNorm_SRgb => SKColorType.Bgra8888,
            PixelFormat.R8G8B8A8_UNorm => SKColorType.Rgba8888,
            PixelFormat.R8G8B8A8_UNorm_SRgb => SKColorType.Rgba8888,

            // 16-bit per channel formats
            PixelFormat.R16G16B16A16_UNorm => SKColorType.Rgba16161616,
            PixelFormat.R16G16B16A16_Float => SKColorType.RgbaF16,

            // 32-bit per channel float formats
            PixelFormat.R32G32B32A32_Float => SKColorType.RgbaF32,

            // 10-bit formats
            PixelFormat.R10G10B10A2_UNorm => SKColorType.Rgba1010102,

            // 16-bit packed formats
            PixelFormat.B5G6R5_UNorm => SKColorType.Rgb565,
            PixelFormat.B5G5R5A1_UNorm => SKColorType.Argb4444,

            // Single channel 8-bit formats
            PixelFormat.R8_UNorm => SKColorType.Gray8,
            PixelFormat.A8_UNorm => SKColorType.Alpha8,

            // Two channel formats
            PixelFormat.R8G8_UNorm => SKColorType.Rg88,
            PixelFormat.R16G16_UNorm => SKColorType.Rg1616,
            PixelFormat.R16G16_Float => SKColorType.RgF16,

            // Unsupported formats
            _ => SKColorType.Unknown
        };
    }

    /// <summary>
    /// Converts a Stride PixelFormat to the equivalent DXGI_FORMAT.
    /// </summary>
    /// <param name="format">The Stride pixel format.</param>
    /// <returns>The equivalent DXGI_FORMAT.</returns>
    /// <exception cref="NotSupportedException">Thrown when the format is not supported.</exception>
    public static DXGI_FORMAT ToDXGIFormat(PixelFormat format) => format switch
    {
        PixelFormat.None => DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,

        // 32-bit per channel formats
        PixelFormat.R32G32B32A32_Typeless => DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_TYPELESS,
        PixelFormat.R32G32B32A32_Float => DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT,
        PixelFormat.R32G32B32A32_UInt => DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_UINT,
        PixelFormat.R32G32B32A32_SInt => DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_SINT,

        // 32-bit per channel RGB formats
        PixelFormat.R32G32B32_Typeless => DXGI_FORMAT.DXGI_FORMAT_R32G32B32_TYPELESS,
        PixelFormat.R32G32B32_Float => DXGI_FORMAT.DXGI_FORMAT_R32G32B32_FLOAT,
        PixelFormat.R32G32B32_UInt => DXGI_FORMAT.DXGI_FORMAT_R32G32B32_UINT,
        PixelFormat.R32G32B32_SInt => DXGI_FORMAT.DXGI_FORMAT_R32G32B32_SINT,

        // 16-bit per channel formats
        PixelFormat.R16G16B16A16_Typeless => DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_TYPELESS,
        PixelFormat.R16G16B16A16_Float => DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT,
        PixelFormat.R16G16B16A16_UNorm => DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM,
        PixelFormat.R16G16B16A16_UInt => DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UINT,
        PixelFormat.R16G16B16A16_SNorm => DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SNORM,
        PixelFormat.R16G16B16A16_SInt => DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SINT,

        // 32-bit two-channel formats
        PixelFormat.R32G32_Typeless => DXGI_FORMAT.DXGI_FORMAT_R32G32_TYPELESS,
        PixelFormat.R32G32_Float => DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
        PixelFormat.R32G32_UInt => DXGI_FORMAT.DXGI_FORMAT_R32G32_UINT,
        PixelFormat.R32G32_SInt => DXGI_FORMAT.DXGI_FORMAT_R32G32_SINT,

        // Depth/stencil formats
        PixelFormat.R32G8X24_Typeless => DXGI_FORMAT.DXGI_FORMAT_R32G8X24_TYPELESS,
        PixelFormat.D32_Float_S8X24_UInt => DXGI_FORMAT.DXGI_FORMAT_D32_FLOAT_S8X24_UINT,
        PixelFormat.R32_Float_X8X24_Typeless => DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS,
        PixelFormat.X32_Typeless_G8X24_UInt => DXGI_FORMAT.DXGI_FORMAT_X32_TYPELESS_G8X24_UINT,

        // 10-bit formats
        PixelFormat.R10G10B10A2_Typeless => DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_TYPELESS,
        PixelFormat.R10G10B10A2_UNorm => DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM,
        PixelFormat.R10G10B10A2_UInt => DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UINT,
        PixelFormat.R11G11B10_Float => DXGI_FORMAT.DXGI_FORMAT_R11G11B10_FLOAT,

        // 8-bit per channel formats
        PixelFormat.R8G8B8A8_Typeless => DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_TYPELESS,
        PixelFormat.R8G8B8A8_UNorm => DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
        PixelFormat.R8G8B8A8_UNorm_SRgb => DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,
        PixelFormat.R8G8B8A8_UInt => DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UINT,
        PixelFormat.R8G8B8A8_SNorm => DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_SNORM,
        PixelFormat.R8G8B8A8_SInt => DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_SINT,

        // 16-bit two-channel formats
        PixelFormat.R16G16_Typeless => DXGI_FORMAT.DXGI_FORMAT_R16G16_TYPELESS,
        PixelFormat.R16G16_Float => DXGI_FORMAT.DXGI_FORMAT_R16G16_FLOAT,
        PixelFormat.R16G16_UNorm => DXGI_FORMAT.DXGI_FORMAT_R16G16_UNORM,
        PixelFormat.R16G16_UInt => DXGI_FORMAT.DXGI_FORMAT_R16G16_UINT,
        PixelFormat.R16G16_SNorm => DXGI_FORMAT.DXGI_FORMAT_R16G16_SNORM,
        PixelFormat.R16G16_SInt => DXGI_FORMAT.DXGI_FORMAT_R16G16_SINT,

        // 32-bit single-channel formats
        PixelFormat.R32_Typeless => DXGI_FORMAT.DXGI_FORMAT_R32_TYPELESS,
        PixelFormat.D32_Float => DXGI_FORMAT.DXGI_FORMAT_D32_FLOAT,
        PixelFormat.R32_Float => DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT,
        PixelFormat.R32_UInt => DXGI_FORMAT.DXGI_FORMAT_R32_UINT,
        PixelFormat.R32_SInt => DXGI_FORMAT.DXGI_FORMAT_R32_SINT,

        // 24-bit depth formats
        PixelFormat.R24G8_Typeless => DXGI_FORMAT.DXGI_FORMAT_R24G8_TYPELESS,
        PixelFormat.D24_UNorm_S8_UInt => DXGI_FORMAT.DXGI_FORMAT_D24_UNORM_S8_UINT,
        PixelFormat.R24_UNorm_X8_Typeless => DXGI_FORMAT.DXGI_FORMAT_R24_UNORM_X8_TYPELESS,
        PixelFormat.X24_Typeless_G8_UInt => DXGI_FORMAT.DXGI_FORMAT_X24_TYPELESS_G8_UINT,

        // 8-bit two-channel formats
        PixelFormat.R8G8_Typeless => DXGI_FORMAT.DXGI_FORMAT_R8G8_TYPELESS,
        PixelFormat.R8G8_UNorm => DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM,
        PixelFormat.R8G8_UInt => DXGI_FORMAT.DXGI_FORMAT_R8G8_UINT,
        PixelFormat.R8G8_SNorm => DXGI_FORMAT.DXGI_FORMAT_R8G8_SNORM,
        PixelFormat.R8G8_SInt => DXGI_FORMAT.DXGI_FORMAT_R8G8_SINT,

        // 16-bit single-channel formats
        PixelFormat.R16_Typeless => DXGI_FORMAT.DXGI_FORMAT_R16_TYPELESS,
        PixelFormat.R16_Float => DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT,
        PixelFormat.D16_UNorm => DXGI_FORMAT.DXGI_FORMAT_D16_UNORM,
        PixelFormat.R16_UNorm => DXGI_FORMAT.DXGI_FORMAT_R16_UNORM,
        PixelFormat.R16_UInt => DXGI_FORMAT.DXGI_FORMAT_R16_UINT,
        PixelFormat.R16_SNorm => DXGI_FORMAT.DXGI_FORMAT_R16_SNORM,
        PixelFormat.R16_SInt => DXGI_FORMAT.DXGI_FORMAT_R16_SINT,

        // 8-bit single-channel formats
        PixelFormat.R8_Typeless => DXGI_FORMAT.DXGI_FORMAT_R8_TYPELESS,
        PixelFormat.R8_UNorm => DXGI_FORMAT.DXGI_FORMAT_R8_UNORM,
        PixelFormat.R8_UInt => DXGI_FORMAT.DXGI_FORMAT_R8_UINT,
        PixelFormat.R8_SNorm => DXGI_FORMAT.DXGI_FORMAT_R8_SNORM,
        PixelFormat.R8_SInt => DXGI_FORMAT.DXGI_FORMAT_R8_SINT,
        PixelFormat.A8_UNorm => DXGI_FORMAT.DXGI_FORMAT_A8_UNORM,
        PixelFormat.R1_UNorm => DXGI_FORMAT.DXGI_FORMAT_R1_UNORM,

        // Special formats
        PixelFormat.R9G9B9E5_Sharedexp => DXGI_FORMAT.DXGI_FORMAT_R9G9B9E5_SHAREDEXP,
        PixelFormat.R8G8_B8G8_UNorm => DXGI_FORMAT.DXGI_FORMAT_R8G8_B8G8_UNORM,
        PixelFormat.G8R8_G8B8_UNorm => DXGI_FORMAT.DXGI_FORMAT_G8R8_G8B8_UNORM,

        // Block-compression formats
        PixelFormat.BC1_Typeless => DXGI_FORMAT.DXGI_FORMAT_BC1_TYPELESS,
        PixelFormat.BC1_UNorm => DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM,
        PixelFormat.BC1_UNorm_SRgb => DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB,
        PixelFormat.BC2_Typeless => DXGI_FORMAT.DXGI_FORMAT_BC2_TYPELESS,
        PixelFormat.BC2_UNorm => DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM,
        PixelFormat.BC2_UNorm_SRgb => DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB,
        PixelFormat.BC3_Typeless => DXGI_FORMAT.DXGI_FORMAT_BC3_TYPELESS,
        PixelFormat.BC3_UNorm => DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM,
        PixelFormat.BC3_UNorm_SRgb => DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB,
        PixelFormat.BC4_Typeless => DXGI_FORMAT.DXGI_FORMAT_BC4_TYPELESS,
        PixelFormat.BC4_UNorm => DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM,
        PixelFormat.BC4_SNorm => DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM,
        PixelFormat.BC5_Typeless => DXGI_FORMAT.DXGI_FORMAT_BC5_TYPELESS,
        PixelFormat.BC5_UNorm => DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM,
        PixelFormat.BC5_SNorm => DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM,

        // BGR formats
        PixelFormat.B5G6R5_UNorm => DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM,
        PixelFormat.B5G5R5A1_UNorm => DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM,
        PixelFormat.B8G8R8A8_UNorm => DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
        PixelFormat.B8G8R8X8_UNorm => DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM,
        PixelFormat.R10G10B10_Xr_Bias_A2_UNorm => DXGI_FORMAT.DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM,
        PixelFormat.B8G8R8A8_Typeless => DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_TYPELESS,
        PixelFormat.B8G8R8A8_UNorm_SRgb => DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM_SRGB,
        PixelFormat.B8G8R8X8_Typeless => DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_TYPELESS,
        PixelFormat.B8G8R8X8_UNorm_SRgb => DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM_SRGB,

        // BC6H and BC7 formats
        PixelFormat.BC6H_Typeless => DXGI_FORMAT.DXGI_FORMAT_BC6H_TYPELESS,
        PixelFormat.BC6H_Uf16 => DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16,
        PixelFormat.BC6H_Sf16 => DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16,
        PixelFormat.BC7_Typeless => DXGI_FORMAT.DXGI_FORMAT_BC7_TYPELESS,
        PixelFormat.BC7_UNorm => DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM,
        PixelFormat.BC7_UNorm_SRgb => DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB,

        // ETC formats (not supported in DXGI, throw exception)
        PixelFormat.ETC1 => throw new NotSupportedException($"PixelFormat {format} is not supported in DXGI."),
        PixelFormat.ETC2_RGB => throw new NotSupportedException($"PixelFormat {format} is not supported in DXGI."),
        PixelFormat.ETC2_RGBA => throw new NotSupportedException($"PixelFormat {format} is not supported in DXGI."),
        PixelFormat.ETC2_RGB_A1 => throw new NotSupportedException($"PixelFormat {format} is not supported in DXGI."),
        PixelFormat.EAC_R11_Unsigned => throw new NotSupportedException($"PixelFormat {format} is not supported in DXGI."),
        PixelFormat.EAC_R11_Signed => throw new NotSupportedException($"PixelFormat {format} is not supported in DXGI."),
        PixelFormat.EAC_RG11_Unsigned => throw new NotSupportedException($"PixelFormat {format} is not supported in DXGI."),
        PixelFormat.EAC_RG11_Signed => throw new NotSupportedException($"PixelFormat {format} is not supported in DXGI."),
        PixelFormat.ETC2_RGBA_SRgb => throw new NotSupportedException($"PixelFormat {format} is not supported in DXGI."),
        PixelFormat.ETC2_RGB_SRgb => throw new NotSupportedException($"PixelFormat {format} is not supported in DXGI."),

        _ => throw new NotSupportedException($"PixelFormat {format} is not supported.")
    };

    /// <summary>
    /// Checks if the given pixel format is supported for Skia rendering.
    /// </summary>
    /// <param name="format">The Stride pixel format.</param>
    /// <returns>True if the format is supported, false otherwise.</returns>
    public static bool IsSupported(PixelFormat format)
    {
        return ToSKColorType(format) != SKColorType.Unknown;
    }
}

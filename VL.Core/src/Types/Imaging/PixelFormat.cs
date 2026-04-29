namespace VL.Lib.Basics.Imaging
{
    // IMPORTANT: Only add new entries to the end of the list to avoid having to update libraries.
    /// <summary>
    /// An enumeration of commonly used pixel formats.
    /// </summary>
    public enum PixelFormat
    {
        /// <summary>
        /// Unkown pixel format.
        /// </summary>
        Unknown,
        /// <summary>
        /// A single-component, 8-bit unsigned-normalized-integer format that supports 8 bits for the red channel.
        /// </summary>
        R8,
        /// <summary>
        /// A single-component, 16-bit unsigned-normalized-integer format that supports 16 bits for the red channel.
        /// </summary>
        R16,
        /// <summary>
        /// A single-component, 32-bit floating-point format that supports 32 bits for the red channel.
        /// </summary>
        R32F,
        /// <summary>
        /// 24-bit RGB pixel format using 8 bits for each channel.
        /// </summary>
        R8G8B8,
        /// <summary>
        /// 24-bit BGR pixel format using 8 bits for each channel.
        /// </summary>
        B8G8R8,
        /// <summary>
        /// 32-bit RGBx pixel format using 8 bits for each channel.
        /// </summary>
        R8G8B8X8,
        /// <summary>
        /// 32-bit RGBA pixel format using 8 bits for each channel.
        /// </summary>
        R8G8B8A8,
        /// <summary>
        /// 32-bit BGRx pixel format using 8 bits for each color channel.
        /// </summary>
        B8G8R8X8,
        /// <summary>
        /// 32-bit BGRA pixel format using 8 bits for each color channel.
        /// </summary>
        B8G8R8A8,
        /// <summary>
        /// 128-bit RGBA floating point pixel format using 32 bits for each channel.
        /// </summary>
        R32G32B32A32F,
        /// <summary>
        /// A two-component, 64-bit floating-point format using 32 bits for each channel.
        /// </summary>
        R32G32F,
        /// <summary>
        /// 64-bit RGBA floating point pixel format using 16 bits for each channel.
        /// </summary>
        R16G16B16A16F,
        /// <summary>
        /// 8-bit 4:2:2 packed YUV format. Single-plane format with Y sample at every pixel and U,V sampled at every second pixel horizontally.
        /// Byte order: U0, Y0, V0, Y1 (2 pixels in 4 bytes).
        /// </summary>
        UYVY,
        /// <summary>
        /// 8-bit 4:2:2 packed YUV format. Single-plane format with Y sample at every pixel and U,V sampled at every second pixel horizontally.
        /// Byte order: Y0, U0, Y1, V0 (2 pixels in 4 bytes). Similar to UYVY but different byte order.
        /// </summary>
        YUY2,
        /// <summary>
        /// 8-bit 4:2:0 semi-planar YUV format. Two planes: Y plane (full resolution) and interleaved UV plane (half width and height).
        /// Most common video format. Requires multi-plane texture views for GPU rendering.
        /// </summary>
        NV12,
        /// <summary>
        /// 8-bit 4:4:4 packed YUVA format. Single-plane format with full resolution for all channels including alpha.
        /// Byte order: V, U, Y, A.
        /// </summary>
        AYUV,
        /// <summary>
        /// 8-bit 4:2:2:4 YUV format with alpha. Two planes: UYVY color plane and separate alpha plane.
        /// Requires multi-plane access.
        /// </summary>
        UYVA,
        /// <summary>
        /// 8-bit 4:2:0 planar YUV format. Three planes: Y (full resolution), V (half width/height), U (half width/height).
        /// Planes in order: Y, V, U. Requires multi-plane texture views for GPU rendering.
        /// </summary>
        YV12,
        /// <summary>
        /// 8-bit 4:2:0 planar YUV format. Three planes: Y (full resolution), U (half width/height), V (half width/height).
        /// Planes in order: Y, U, V. Similar to YV12 but with U and V planes swapped.
        /// </summary>
        I420,
        /// <summary>
        /// 10-bit 4:2:0 semi-planar YUV format (stored in 16 bits per component). Two planes: Y plane and interleaved UV plane.
        /// Used for HDR video. Lower 6 bits should be zero. Requires multi-plane texture views for GPU rendering.
        /// </summary>
        P010,
        /// <summary>
        /// 16-bit 4:2:0 semi-planar YUV format. Two planes: Y plane (full resolution) and interleaved UV plane (half width/height).
        /// Used for high-precision HDR video. Requires multi-plane texture views for GPU rendering.
        /// </summary>
        P016,
        /// <summary>
        /// 16-bit 4:2:2 semi-planar YUV format. Two planes: Y plane (full resolution) and interleaved UV plane (half horizontal resolution).
        /// Used for high-precision video. Requires multi-plane texture views for GPU rendering.
        /// </summary>
        P216
    }
}

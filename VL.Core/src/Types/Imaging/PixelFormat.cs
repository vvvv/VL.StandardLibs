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
        R16G16B16A16F
    }
}

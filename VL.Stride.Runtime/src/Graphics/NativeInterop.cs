using Stride.Graphics;
using VL.Lib.Basics.Video;

namespace VL.Stride.Graphics;

/// <summary>
/// Gives access to the native graphics objects behind Stride's graphics types as plain pointers.
/// This is the stable API for libraries that need to hand the device or a texture to native code or to their own
/// interop layer (Vortice, TerraFX, raw COM, ...). It doesn't expose any wrapper types, so it keeps working
/// when Stride changes the wrapper library it uses internally.
/// </summary>
/// <remarks>
/// A pointer is only meaningful together with the graphics API it belongs to. Therefore the <c>TryGet</c> methods take
/// the API the caller is able to work with and only succeed if the device actually uses it, just like a type check
/// on a wrapper object would. For <see cref="GraphicsDeviceType.Direct3D11"/> the pointers are <c>ID3D11Device*</c>,
/// <c>ID3D11DeviceContext*</c> and <c>ID3D11Resource*</c> (for a texture query it for <c>ID3D11Texture2D</c>).
/// <para>
/// The pointers are borrowed: the reference count isn't incremented, so call <c>AddRef</c> if you need to keep one beyond
/// the lifetime of the Stride object it was taken from. The native device can change at runtime (e.g. on device reset),
/// so ask for it again instead of caching it.
/// </para>
/// </remarks>
public static class NativeInterop
{
    /// <summary>
    /// The graphics API the device uses, <see cref="GraphicsDeviceType.None"/> if it's not one that can be accessed natively.
    /// </summary>
    public static GraphicsDeviceType GetGraphicsDeviceType(this GraphicsDevice graphicsDevice)
        => GraphicsDevice.Platform == GraphicsPlatform.Direct3D11 ? GraphicsDeviceType.Direct3D11 : GraphicsDeviceType.None;

    /// <summary>
    /// Gets the native device pointer if the device uses the given graphics API.
    /// </summary>
    public static bool TryGetNativeDevicePointer(this GraphicsDevice graphicsDevice, GraphicsDeviceType type, out nint pointer)
    {
        pointer = default;
        return type != GraphicsDeviceType.None
            && graphicsDevice.GetGraphicsDeviceType() == type
            && SharpDXInterop.GetNativeDevice(graphicsDevice) is SharpDX.ComObject device
            && (pointer = device.NativePointer) != default;
    }

    /// <summary>
    /// Gets the native immediate device context pointer if the device uses the given graphics API.
    /// </summary>
    public static bool TryGetNativeDeviceContextPointer(this GraphicsDevice graphicsDevice, GraphicsDeviceType type, out nint pointer)
    {
        pointer = default;
        return type != GraphicsDeviceType.None
            && graphicsDevice.GetGraphicsDeviceType() == type
            && SharpDXInterop.GetNativeDeviceContext(graphicsDevice) is SharpDX.ComObject context
            && (pointer = context.NativePointer) != default;
    }

    /// <inheritdoc cref="TryGetNativeDeviceContextPointer(GraphicsDevice, GraphicsDeviceType, out nint)"/>
    public static bool TryGetNativeDeviceContextPointer(this CommandList commandList, GraphicsDeviceType type, out nint pointer)
        => commandList.GraphicsDevice.TryGetNativeDeviceContextPointer(type, out pointer);

    /// <summary>
    /// Gets the native resource pointer if the resource's device uses the given graphics API.
    /// </summary>
    public static bool TryGetNativeResourcePointer(this GraphicsResource graphicsResource, GraphicsDeviceType type, out nint pointer)
    {
        pointer = default;
        return type != GraphicsDeviceType.None
            && graphicsResource.GraphicsDevice?.GetGraphicsDeviceType() == type
            && SharpDXInterop.GetNativeResource(graphicsResource) is SharpDX.ComObject resource
            && (pointer = resource.NativePointer) != default;
    }
}

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Stride.Graphics;
using Buffer = Stride.Graphics.Buffer;

namespace VL.Stride.Shaders
{
    public static class NativeDeviceUtils
    {
        public static ComPtr<ID3D11DeviceContext> GetNativeDeviceContext(this CommandList commandList)
        {
            return GetNativeDeviceContext(commandList.GraphicsDevice);
        }

        public static ComPtr<ID3D11DeviceContext> GetNativeDeviceContext(this GraphicsDevice graphicsDevice)
        {
            return GraphicsMarshal.GetNativeDeviceContext(graphicsDevice);
        }

        public static ComPtr<ID3D11Device> GetNativeDevice(this GraphicsDevice graphicsDevice)
        {
            return GraphicsMarshal.GetNativeDevice(graphicsDevice);
        }

        public static ComPtr<ID3D11Resource> GetNativeResource(this GraphicsResource graphicsResource)
        {
            return GraphicsMarshal.GetNativeResource(graphicsResource);
        }

        public static void DrawInstancedIndirect(this CommandList commandList, Buffer argsBuffer, int alignedByteOffsetForArgs)
        {
            var buffer = argsBuffer.NativeBuffer;
            commandList.GetNativeDeviceContext().DrawInstancedIndirect(buffer, (uint)alignedByteOffsetForArgs);
        }

        public static void DispatchIndirect(this CommandList commandList, Buffer argsBuffer, int alignedByteOffsetForArgs)
        {
            var buffer = argsBuffer.NativeBuffer;
            commandList.GetNativeDeviceContext().DispatchIndirect(buffer, (uint)alignedByteOffsetForArgs);
        }
    }
}

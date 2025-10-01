using Stride.Graphics;
using System.Runtime.CompilerServices;

namespace VL.Stride.Rendering
{
    internal static class CommandListExt
    {
        internal static void UpdateSubresource(this CommandList commandList, GraphicsResource resource, int subResourceIndex, DataBox databox)
        {
            UpdateSubresource(commandList, resource, subResourceIndex, databox);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(UpdateSubresource))]
            extern static void UpdateSubresource(CommandList commandList, GraphicsResource resource, int subResourceIndex, DataBox databox);
        }
    }
}

namespace Stride.Input
{
    using Stride.Core.Collections;
    using Stride.Core.Mathematics;
    using System.Windows.Input;

    internal static class TransfomHelper
    {
        internal static Vector2 transformPos(this Vector2 pos, IPointerDevice mouse, MappedInputSource source)
        {
            return ((pos * mouse.SurfaceSize) - new Vector2(source.Viewport.X, source.Viewport.Y)) / source.Viewport.Size;
        }
        internal static Vector2 transformDelta(this Vector2 delta, IPointerDevice mouse, MappedInputSource source)
        {
            return delta * mouse.SurfaceSize / source.Viewport.Size;
        }
    }
}

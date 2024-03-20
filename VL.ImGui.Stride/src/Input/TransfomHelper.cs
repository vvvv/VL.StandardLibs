namespace Stride.Input
{
    using Stride.Core.Collections;
    using Stride.Core.Mathematics;

    internal static class TransfomHelper
    {
        internal static IReadOnlySet<PointerPoint> transform(this IReadOnlySet<PointerPoint> pointers, IPointerDevice pointer, MappedInputSource source, IPointerDevice device)
        {
            return new ReadOnlySet<PointerPoint>(pointers.Select(point =>
            {
                return new PointerPoint()
                {
                    Position = point.Position.transformPos(pointer, source),
                    Delta = point.Delta.transformDelta(pointer, source),
                    IsDown = point.IsDown,
                    Id = point.Id,
                    Pointer = device
                };
            }).ToHashSet());
        }
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

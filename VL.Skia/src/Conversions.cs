using SkiaSharp;
using System.Runtime.CompilerServices;
using Stride.Core.Mathematics;

namespace VL.Skia
{
    public static class Conversions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref SKSize AsSKSize(ref Vector2 input) => ref Unsafe.As<Vector2, SKSize>(ref input);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref SKPoint AsSKPoint(ref Vector2 input) => ref Unsafe.As<Vector2, SKPoint>(ref input);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref SKPoint3 AsSKPoint3(ref Vector3 input) => ref Unsafe.As<Vector3, SKPoint3>(ref input);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref Vector2 AsVector2(ref SKPoint input) => ref Unsafe.As<SKPoint, Vector2>(ref input);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref Vector3 AsVector3(ref SKPoint3 input) => ref Unsafe.As<SKPoint3, Vector3>(ref input);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SKColor ToSKColor(ref Color4 input)
        {
            input.ToBgra(out var r, out var g, out var b, out var a);
            return new SKColor(r, g, b, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 ToColor(ref SKColor input)
        {
            var rgba = input.Alpha << 24 | input.Blue << 16 | input.Green << 8 | input.Red;
            return new Color4(rgba);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SKRect ToSKRect(ref RectangleF input) => new SKRect(input.Left, input.Top, input.Right, input.Bottom);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SKRect ToSKRect_(this RectangleF input) => new SKRect(input.Left, input.Top, input.Right, input.Bottom);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RectangleF ToRectangleF(ref SKRect input) => new RectangleF(input.Left, input.Top, input.Width, input.Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RectangleF ToRectangleF_(this SKRect input) => new RectangleF(input.Left, input.Top, input.Width, input.Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SKRectI ToSKRectI(ref Rectangle input) => new SKRectI(input.Left, input.Top, input.Right, input.Bottom);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rectangle ToRectangle(ref SKRectI input) => new Rectangle(input.Left, input.Top, input.Width, input.Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Drawing.Rectangle ToRectangle(ref Stride.Core.Mathematics.RectangleF input) => 
            new System.Drawing.Rectangle((int)input.Left, (int)input.Top, (int)input.Width, (int)input.Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RectangleF ToRectangleF(ref System.Drawing.Rectangle input) =>
            new RectangleF(input.Left, input.Top, input.Width, input.Height);
    }
}

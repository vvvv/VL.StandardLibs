using ImGuiNET;
using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;

namespace VL.ImGui
{
    public static class ImGuiConversion
    {
        public static float ToImGuiScaling = VL.UI.Core.DIPHelpers.DIPFactor() * 100f;
        public static float FromImGuiScaling = 1.0f / (VL.UI.Core.DIPHelpers.DIPFactor() * 100f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector4 ToImGui(this Color4 v)
        {
            return Unsafe.As<Color4, System.Numerics.Vector4>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector2 ToImGui(this Vector2 v)
        {
            return Unsafe.As<Vector2, System.Numerics.Vector2>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector2 FromHectoToImGui(this Vector2 v)
        {
            v = new Vector2(v.X * ToImGuiScaling, v.Y * ToImGuiScaling);
            return Unsafe.As<Vector2, System.Numerics.Vector2>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector3 ToImGui(this Vector3 v)
        {
            return Unsafe.As<Vector3, System.Numerics.Vector3>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector4 ToImGui(this Vector4 v)
        {
            return Unsafe.As<Vector4, System.Numerics.Vector4>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector2 FromHectoToImGui(this Size2F v)
        {
            v = new Size2F(v.Width * ToImGuiScaling, v.Height * ToImGuiScaling);
            return Unsafe.As<Size2F, System.Numerics.Vector2>(ref v);
        }

        public static float FromHectoToImGui(this float v) => v * ToImGuiScaling;

        //TO VL

        public static float ToVLHecto(this float v) => v * FromImGuiScaling;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVLHecto(this System.Numerics.Vector2 v)
        {
            v = new System.Numerics.Vector2(v.X * FromImGuiScaling, v.Y * FromImGuiScaling);
            return Unsafe.As<System.Numerics.Vector2, Vector2>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVL(this System.Numerics.Vector2 v)
        {
            return Unsafe.As<System.Numerics.Vector2, Vector2>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVL(this System.Numerics.Vector3 v)
        {
            return Unsafe.As<System.Numerics.Vector3, Vector3>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ToVL(this System.Numerics.Vector4 v)
        {
            return Unsafe.As<System.Numerics.Vector4, Vector4>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 ToVLColor4(this System.Numerics.Vector4 v)
        {
            return Unsafe.As<System.Numerics.Vector4, Color4>(ref v);
        }

        public static IEnumerable<TOut> Select<T, TOut>(this RangeAccessor<T> range, Func<T, TOut> selector) where T : struct
        {
            for (int i = 0; i < range.Count; i++)
                yield return selector(range[i]);
        }
    }

}

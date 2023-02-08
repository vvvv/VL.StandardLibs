using ImGuiNET;
using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;

namespace VL.ImGui.Widgets.Primitives
{
    [GenerateNode (Category = "ImGui.Primitives")]
    internal partial class Line : PrimitiveWidget
    {
        public IReadOnlyList<Vector2> Points { private get; set; } = Array.Empty<Vector2>();

        public Color4 Color { private get; set; } = Color4.White;

        public bool IsClosed { private get; set; } = false;

        public float Thickness { private get; set; } = 0.01f;

        protected override void Draw(Context context, in ImDrawListPtr drawList, in System.Numerics.Vector2 offset)
        {
            var points = Points;
            var color = (uint)Color.ToRgba();

            // See comment in base class about matrix transforms to make use of this code path. 
            // For now we'd need to apply the offset manually on each point thereby probably defeating the purpose.
            //if (points.TryGetSpan(out var vectors))
            //{
            //    // Fast code path
            //    ref var x = ref Unsafe.As<Vector2, System.Numerics.Vector2>(ref Unsafe.AsRef(in vectors[0]));
            //    drawList.AddPolyline(ref x, vectors.Length, color, IsClosed ? ImDrawFlags.Closed : ImDrawFlags.None, Thickness.FromHectoToImGui());
            //}
            //else
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    drawList.AddLine(points[i].FromHectoToImGui() + offset, points[i + 1].FromHectoToImGui() + offset, color, Thickness.FromHectoToImGui());
                }

                if (IsClosed && points.Count > 1)
                {
                    drawList.AddLine(points[points.Count - 1].FromHectoToImGui() + offset, points[0].FromHectoToImGui() + offset, color, Thickness.FromHectoToImGui());
                }
            }
        }
    }
}

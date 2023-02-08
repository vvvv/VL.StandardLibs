using ImGuiNET;
using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets.Primitives
{
    [GenerateNode(Category = "ImGui.Primitives", Name = "Bezier (Quadratic)")]
    internal partial class BezierQuadratic : PrimitiveWidget
    {
        public Vector2 Point1 { private get; set; } = new Vector2(0, 0);

        public Vector2 Point2 { private get; set; } = new Vector2(1, 0);

        public Vector2 Point3 { private get; set; } = new Vector2(1, 1);

        /// <summary>
        /// Use 0 to automatically calculate tessellation (preferred).
        /// </summary>
        public int SegmentsCount { private get; set; } = 0;

        public Color4 Color { private get; set; } = Color4.White;

        public float Thickness { private get; set; } = 0.01f;

        protected override void Draw(Context context, in ImDrawListPtr drawList, in System.Numerics.Vector2 offset)
        {
            var color = (uint)Color.ToRgba();

            drawList.AddBezierQuadratic(Point1.FromHectoToImGui() + offset, Point2.FromHectoToImGui() + offset, Point3.FromHectoToImGui() + offset, color, 
                Thickness.FromHectoToImGui(), SegmentsCount);
        }
    }
}

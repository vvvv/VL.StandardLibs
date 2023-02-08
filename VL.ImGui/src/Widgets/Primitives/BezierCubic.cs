using ImGuiNET;
using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets.Primitives
{
    [GenerateNode(Category = "ImGui.Primitives", Name = "Bezier (Cubic)")]
    internal partial class BezierCubic : PrimitiveWidget
    {
        public Vector2 Point1 { private get; set; } = new Vector2(0f, 0.5f);

        public Vector2 Point2 { private get; set; } = new Vector2(0.25f, 1f);

        public Vector2 Point3 { private get; set; } = new Vector2(0.75f, 0f);

        public Vector2 Point4 { private get; set; } = new Vector2(1f, 0.5f);

        /// <summary>
        /// Use 0 to automatically calculate tessellation (preferred).
        /// </summary>
        public int SegmentsCount { private get; set; } = 0;

        public Color4 Color { private get; set; } = Color4.White;

        public float Thickness { private get; set; } = 0.01f;

        protected override void Draw(Context context, in ImDrawListPtr drawList, in System.Numerics.Vector2 offset)
        {
            var color = (uint)Color.ToRgba();

            drawList.AddBezierCubic(
                Point1.FromHectoToImGui() + offset, Point2.FromHectoToImGui() + offset, 
                Point3.FromHectoToImGui() + offset, Point4.FromHectoToImGui() + offset, color, Thickness.FromHectoToImGui(), SegmentsCount);
        }
    }
}

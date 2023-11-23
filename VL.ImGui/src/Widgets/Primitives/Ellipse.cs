using ImGuiNET;
using Stride.Core.Mathematics;
using VL.Lib.Collections;

namespace VL.ImGui.Widgets.Primitives
{
    [GenerateNode(Category = "ImGui.Primitives", Tags ="Segment")]
    internal partial class Ellipse : PrimitiveWidget
    {
        public Vector2 Center { private get; set; } = Vector2.Zero;

        public Vector2 Radius { private get; set; } = Vector2.One;

        public int SegmentsCount { private get; set; } = 3;

        public Color4 Color { private get; set; } = Color4.White;

        public float Rotation { private get; set; }

        public bool IsFilled { private get; set; } = false;

        public float Thickness { private get; set; } = 0.01f;

        protected override void Draw(Context context, in ImDrawListPtr drawList, in System.Numerics.Vector2 offset)
        {
            var color = (uint)Color.ToRgba();
            var radius = Radius.FromHectoToImGui();
            if (IsFilled)
            {
                drawList.AddEllipseFilled(Center.FromHectoToImGui(), radius.X, radius.Y, color, Rotation, SegmentsCount);
            }
            else
            {
                drawList.AddEllipse(Center.FromHectoToImGui(), radius.X, radius.Y, color, Rotation, SegmentsCount, Thickness.FromHectoToImGui());
            }
        }
    }
}

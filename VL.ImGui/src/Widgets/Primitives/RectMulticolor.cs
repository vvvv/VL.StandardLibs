using ImGuiNET;
using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets.Primitives
{
    [GenerateNode (Category = "ImGui.Primitives", Name = "Rect (MultiColor)")]
    internal partial class RectMulticolor : PrimitiveWidget
    {
        public Vector2 TopLeft { private get; set; } = Vector2.Zero;

        public Vector2 BottomRight { private get; set; } = new Vector2(1, 1);

        public Color4 TopLeftColor { private get; set; } = Color4.White;
        public Color4 TopRightColor { private get; set; } = Color4.White;
        public Color4 BottomLeftColor { private get; set; } = Color4.White;
        public Color4 BottomRightColor { private get; set; } = Color4.White;

        protected override void Draw(Context context, in ImDrawListPtr drawList, in System.Numerics.Vector2 offset)
        {

            drawList.AddRectFilledMultiColor(TopLeft.FromHectoToImGui() + offset, BottomRight.FromHectoToImGui() + offset, 
                (uint)TopLeftColor.ToRgba(), (uint)TopRightColor.ToRgba(), 
                (uint)BottomRightColor.ToRgba(), (uint)BottomLeftColor.ToRgba());
        }
    }
}

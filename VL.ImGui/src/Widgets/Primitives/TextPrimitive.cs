using ImGuiNET;
using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets.Primitives
{
    [GenerateNode(Category = "ImGui.Primitives", Name = "Text")]
    internal partial class TextPrimitive : PrimitiveWidget
    {
        public Vector2 Position { private get; set; } = Vector2.Zero;

        public string Text { private get; set; } = "[Text]";

        public Color4 Color { private get; set; } = Color4.White;

        protected override void Draw(Context context, in ImDrawListPtr drawList, in System.Numerics.Vector2 offset)
        {
            var color = (uint)Color.ToRgba();

            drawList.AddText(Position.FromHectoToImGui() + offset, color, Text);
        }
    }
}

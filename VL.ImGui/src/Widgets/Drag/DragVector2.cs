using ImGuiNET;
using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (Vector2)", Category = "ImGui.Widgets", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragVector2 : DragWidget<Vector2, float>
    {
        protected override bool Drag(string label, ref Vector2 value, float speed, float min, float max, string? format, ImGuiSliderFlags flags)
        {
            var x = value.ToImGui();
            if (ImGuiNET.ImGui.DragFloat2(label, ref x, speed, min, max, format, flags))
            {
                value = x.ToVL();
                return true;
            }
            return false;
        }
    }
}

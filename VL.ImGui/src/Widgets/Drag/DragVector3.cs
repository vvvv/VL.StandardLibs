using ImGuiNET;
using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (Vector3)", Category = "ImGui.Widgets", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragVector3 : DragWidget<Vector3, float>
    {
        protected override bool Drag(string label, ref Vector3 value, float speed, float min, float max, string? format, ImGuiSliderFlags flags)
        {
            var x = value.ToImGui();
            if (ImGuiNET.ImGui.DragFloat3(label, ref x, speed, min, max, format, flags))
            {
                value = x.ToVL();
                return true;
            }
            return false;
        }
    }
}

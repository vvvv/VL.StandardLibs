using ImGuiNET;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (Float)", Category = "ImGui.Widgets", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragFloat : DragWidget<float, float>
    {
        protected override bool Drag(string label, ref float value, float speed, float min, float max, string? format, ImGuiSliderFlags flags)
        {
            return ImGuiNET.ImGui.DragFloat(label, ref value, speed, min, max, format, flags);
        }
    }
}

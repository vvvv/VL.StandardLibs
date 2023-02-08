using ImGuiNET;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (Int)", Category = "ImGui.Widgets", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragInt : DragWidget<int, int>
    {
        protected override bool Drag(string label, ref int value, float speed, int min, int max, string? format, ImGuiSliderFlags flags)
        {
            return ImGuiNET.ImGui.DragInt(label, ref value, speed, min, max, format, flags);
        }
    }
}

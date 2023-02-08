using ImGuiNET;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (Float64)", Category = "ImGui.Widgets.Advanced", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragFloat64 : DragWidget<double, double>
    {
        protected override bool Drag(string label, ref double value, float speed, double min, double max, string? format, ImGuiSliderFlags flags)
        {
            return ImGuiUtils.DragDouble(label, ref value, speed, min, max, format, flags);
        }
    }
}

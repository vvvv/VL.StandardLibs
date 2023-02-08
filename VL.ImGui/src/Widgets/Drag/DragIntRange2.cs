using ImGuiNET;
using VL.Core.EditorAttributes;
using VL.Lib.Mathematics;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (Int Range)", Category = "ImGui.Widgets", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragIntRange2 : DragWidget<Range<int>, int>
    {
        protected override bool Drag(string label, ref Range<int> value, float speed, int min, int max, string? format, ImGuiSliderFlags flags)
        {
            value.Split(out int from, out int to);
            if (ImGuiNET.ImGui.DragIntRange2(label, ref from, ref to, speed, min, max, format, format, flags))
            {
                value = new Range<int>(from, to);
                return true;
            }
            return false;
        }
    }
}

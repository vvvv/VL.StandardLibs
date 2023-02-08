using ImGuiNET;
using VL.Core.EditorAttributes;
using VL.Lib.Mathematics;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (Float Range)", Category = "ImGui.Widgets", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragFloatRange2 : DragWidget<Range<float>, float>
    {
        protected override bool Drag(string label, ref Range<float> value, float speed, float min, float max, string? format, ImGuiSliderFlags flags)
        {
            value.Split(out float from, out float to);
            if (ImGuiNET.ImGui.DragFloatRange2(label, ref from, ref to, speed, min, max, format, format, flags))
            {
                value = new Range<float>(from, to);
                return true;
            }
            return false;
        }
    }
}

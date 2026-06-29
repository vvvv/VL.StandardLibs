using ImGuiNET;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (Int64)", Category = "ImGui.Widgets", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragInt64 : DragWidget<Int64, Int64>
    {
        public DragInt64()
            : base(int.MinValue, int.MaxValue)  //not Int64.MinValue, Int64.MaxValue. We don't support values outside int range
        {
        }

        protected override bool Drag(string label, ref Int64 value, float speed, Int64 min, Int64 max, string? format, ImGuiSliderFlags flags)
        {
            int intValue = (int)value;
            bool result = ImGuiNET.ImGui.DragInt(label, ref intValue, speed, (int)min, (int)max, format, flags);
            value = intValue;
            return result;
        }
    }
}

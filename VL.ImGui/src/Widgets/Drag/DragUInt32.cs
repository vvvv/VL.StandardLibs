using ImGuiNET;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (UInt32)", Category = "ImGui.Widgets", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragUInt32 : DragWidget<UInt32, UInt32>
    {
        public DragUInt32()
            : base(0, int.MaxValue) //not uint.MaxValue. We don't support values above int.MaxValue 
        {
        }

        protected override bool Drag(string label, ref uint value, float speed, uint min, uint max, string? format, ImGuiSliderFlags flags)
        {
            int intValue = (int)value;
            bool result = ImGuiNET.ImGui.DragInt(label, ref intValue, speed, (int)min, (int)max, format, flags);
            value = (uint)intValue;
            return result;
        }
    }
}

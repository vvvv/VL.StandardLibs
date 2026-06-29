using ImGuiNET;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (UInt64)", Category = "ImGui.Widgets", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragUInt64 : DragWidget<UInt64, UInt64>
    {
        public DragUInt64()
            : base(0, int.MaxValue) //not ulong.MaxValue. We don't support values above long.MaxValue 
        {
        }

        protected override bool Drag(string label, ref ulong value, float speed, ulong min, ulong max, string? format, ImGuiSliderFlags flags)
        {
            int intValue = (int)value;
            bool result = ImGuiNET.ImGui.DragInt(label, ref intValue, speed, (int)min, (int)max, format, flags);
            value = (UInt64)intValue;
            return result;
        }
    }
}

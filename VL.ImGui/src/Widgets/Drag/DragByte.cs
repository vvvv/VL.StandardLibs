using ImGuiNET;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (Byte)", Category = "ImGui.Widgets", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragByte: DragWidget<byte, byte>
    {
        public DragByte()
            : base(0, 255)
        {
        }

        protected override bool Drag(string label, ref byte value, float speed, byte min, byte max, string? format, ImGuiSliderFlags flags)
        {
            int intValue = value;
            bool result = ImGuiNET.ImGui.DragInt(label, ref intValue, speed, min, max, format, flags);
            value = (byte)intValue;
            return result;
        }
    }
}

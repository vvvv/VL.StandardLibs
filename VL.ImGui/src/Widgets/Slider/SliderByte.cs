using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Byte)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.Slider)]
    internal partial class SliderByte : SliderWidget<byte, byte>
    {
        public SliderByte() : base(min: 0, max: 255)
        {
        }

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            int intValue = value;
            if (ImGuiNET.ImGui.SliderInt(widgetLabel.Update(label.Value), ref intValue, min.Value, max.Value, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = (byte)intValue;
        }
    }
}

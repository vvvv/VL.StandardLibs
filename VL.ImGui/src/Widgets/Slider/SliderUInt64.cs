using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Int64 Unsigned)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.Slider)]
    internal partial class SliderUnsignedInt64 : SliderWidget<UInt64, int>
    {
        public SliderUnsignedInt64() : base(min: 0, max: 100)
        {
        }

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            var intValue = (int)value;
            if (ImGuiNET.ImGui.SliderInt(widgetLabel.Update(label.Value), ref intValue,
                min.Value, max.Value, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = (UInt64)intValue;
        }
    }
}

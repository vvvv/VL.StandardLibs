using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Int32 Unsigned)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.Slider)]
    internal partial class SliderUnsignedInt32 : SliderWidget<UInt32, int>
    {
        public SliderUnsignedInt32() : base(min: 0, max: 100)
        {
        }

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            var intValue = (int)value;
            if (ImGuiNET.ImGui.SliderInt(widgetLabel.Update(label.Value), ref intValue,
                min.Value, max.Value, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = (UInt32)intValue;
        }
    }
}

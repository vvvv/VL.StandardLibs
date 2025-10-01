using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Float)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.Slider)]
    internal partial class SliderFloat : SliderWidget<float, float>
    {
        public SliderFloat() : base(min: 0f, max: 1f) 
        {
        }

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiNET.ImGui.SliderFloat(widgetLabel.Update(label.Value), ref value, min.Value, max.Value, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = value;
        }
    }
}

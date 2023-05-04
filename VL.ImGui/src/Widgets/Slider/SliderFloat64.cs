using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Float64)", Category = "ImGui.Widgets.Advanced")]
    [WidgetType(WidgetType.Slider)]
    internal partial class SliderFloat64 : SliderWidget<double, double>
    {
        public SliderFloat64() : base(min: 0d, max: 1d)
        {
        }

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiUtils.SliderDouble(Context.GetLabel(this, Label), ref value, Min, Max, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = value;
        }
    }
}

using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Float64 Vertical)", Category = "ImGui.Widgets.Advanced")]
    [WidgetType(WidgetType.VerticalSlider)]
    internal partial class SliderFloat64Vertical : VerticalSliderWidget<double, double>
    {
        public SliderFloat64Vertical() : base(min: 0d, max: 1d, size: new Vector2(.4f, 1f))
        {
        }

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiUtils.VSliderDouble(Context.GetLabel(this, Label), Size.FromHectoToImGui(), ref value, Min, Max, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = value;
        }
    }
}

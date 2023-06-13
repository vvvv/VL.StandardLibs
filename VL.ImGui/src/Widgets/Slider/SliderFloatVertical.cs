using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Float Vertical)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.VerticalSlider)]
    internal partial class SliderFloatVertical : VerticalSliderWidget<float, float>
    {
        public SliderFloatVertical() : base(min: 0f, max: 1f, size: new Vector2(.4f, 1f))
        {
        }

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiNET.ImGui.VSliderFloat(widgetLabel.Update(Label), Size.FromHectoToImGui(), ref value, Min, Max, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = value;
        }
    }
}

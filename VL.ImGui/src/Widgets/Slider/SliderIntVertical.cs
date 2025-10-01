using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Int Vertical)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.Slider)]
    internal partial class SliderIntVertical : VerticalSliderWidget<int, int>
    {
        public SliderIntVertical() : base(min: 0, max: 100, size: new Vector2(.2f, 1f))
        {
        }

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiNET.ImGui.VSliderInt(widgetLabel.Update(label.Value), Size.FromHectoToImGui(), ref value, min.Value, max.Value, Format, Flags))
                Value = value;
        }
    }
}

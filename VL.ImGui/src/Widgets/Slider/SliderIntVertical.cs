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
            if (ImGuiNET.ImGui.VSliderInt(Context.GetLabel(this, Label), Size.FromHectoToImGui(), ref value, Min, Max, Format, Flags))
                Value = value;
        }
    }
}

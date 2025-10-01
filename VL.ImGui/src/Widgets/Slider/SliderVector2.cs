using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Vector2)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.Slider)]
    internal partial class SliderVector2 : SliderWidget<Vector2, float>
    {
        public SliderVector2() : base(min: 0f, max: 1f)
        {
        }

        internal override void UpdateCore(Context context)
        {
            var value = Update().ToImGui();
            if (ImGuiNET.ImGui.SliderFloat2(widgetLabel.Update(label.Value), ref value, min.Value, max.Value, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = value.ToVL();
        }

    }
}

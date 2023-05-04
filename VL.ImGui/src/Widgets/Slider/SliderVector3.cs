using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Vector3)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.Slider)]
    internal partial class SliderVector3 : SliderWidget<Vector3, float>
    {
        public SliderVector3() : base(min: 0f, max: 1f)
        {
        }

        internal override void UpdateCore(Context context)
        {
            var value = Update().ToImGui();
            if (ImGuiNET.ImGui.SliderFloat3(Context.GetLabel(this, Label), ref value, Min, Max, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = value.ToVL();
        }
    }
}

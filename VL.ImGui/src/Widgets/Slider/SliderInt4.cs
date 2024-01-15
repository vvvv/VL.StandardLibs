using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Int4)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.Slider)]
    internal partial class SliderInt4 : SliderWidget<Int4, int>
    {
        public SliderInt4() : base(min: 0, max: 100)
        {
        }

        internal override void UpdateCore(Context context)
        {
            var value = Update();

            ref var x = ref value.X;
            if (ImGuiNET.ImGui.SliderInt4(widgetLabel.Update(Label), ref x, Min, Max, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = Unsafe.As<int, Int4>(ref x);
        }
    }
}

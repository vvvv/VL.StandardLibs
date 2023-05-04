using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Int2)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.Slider)]
    internal partial class SliderInt2 : SliderWidget<Int2, int>
    {
        public SliderInt2() : base(min: 0, max: 100)
        {
        }

        internal override void UpdateCore(Context context)
        {
            var value = Update();

            ref var x = ref value.X;
            if (ImGuiNET.ImGui.SliderInt2(Context.GetLabel(this, Label), ref x, Min, Max, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = Unsafe.As<int, Int2>(ref x);
        }
    }
}

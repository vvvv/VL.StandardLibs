using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Int2)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.Slider)]
    internal partial class SliderInt2 : ChannelWidget<Int2>
    {
        public string? Label { get; set; }

        public int Min { private get; set; } = 0;

        public int Max { private get; set; } = 100;

        public string? Format { private get; set; }

        public ImGuiNET.ImGuiSliderFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var value = Update();

            ref var x = ref value.X;
            if (ImGuiNET.ImGui.SliderInt2(Context.GetLabel(this, Label), ref x, Min, Max, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = Unsafe.As<int, Int2>(ref x);
        }
    }
}

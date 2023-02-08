using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Int Vertical)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.Slider)]
    internal partial class SliderIntVertical : ChannelWidget<int>
    {
        public string? Label { get; set; }

        public int Min { private get; set; } = 0;

        public int Max { private get; set; } = 100;

        public Vector2 Size { get; set; } = new Vector2(.2f, 1f);

        public string? Format { private get; set; }

        public ImGuiNET.ImGuiSliderFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiNET.ImGui.VSliderInt(Context.GetLabel(this, Label), Size.FromHectoToImGui(), ref value, Min, Max, Format, Flags))
                Value = value;
        }
    }
}

using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Int)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.Slider)]
    internal partial class SliderInt : ChannelWidget<int>
    {
        public string? Label { get; set; }

        public int Min { private get; set; } = 0;

        public int Max { private get; set; } = 100;

        public string? Format { private get; set; }

        public ImGuiNET.ImGuiSliderFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiNET.ImGui.SliderInt(Context.GetLabel(this, Label), ref value, Min, Max, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = value;
        }
    }
}

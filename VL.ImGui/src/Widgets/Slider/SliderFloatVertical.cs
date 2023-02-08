using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Float Vertical)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.VerticalSlider)]
    internal partial class SliderFloatVertical : ChannelWidget<float>
    {
        public string? Label { get; set; }

        public float Min { private get; set; } = 0f;

        public float Max { private get; set; } = 1f;

        public Vector2 Size { get; set; } = new Vector2 (.4f, 1f);

        /// <summary>
        /// Adjust format string to decorate the value with a prefix, a suffix, or adapt the editing and display precision e.g. "%.3f" -> 1.234; "%5.2f secs" -> 01.23 secs; "Biscuit: % .0f" -> Biscuit: 1; etc.
        /// </summary>
        public string? Format { private get; set; }

        public ImGuiNET.ImGuiSliderFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiNET.ImGui.VSliderFloat(Context.GetLabel(this, Label), Size.FromHectoToImGui(), ref value, Min, Max, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = value;
        }
    }
}

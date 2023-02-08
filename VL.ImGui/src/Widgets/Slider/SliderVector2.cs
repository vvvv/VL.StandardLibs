using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Slider (Vector2)", Category = "ImGui.Widgets")]
    [WidgetType(WidgetType.Slider)]
    internal partial class SliderVector2 : ChannelWidget<Vector2>
    {
        public string? Label { get; set; }

        public float Min { private get; set; } = 0f;

        public float Max { private get; set; } = 1f;

        /// <summary>
        /// Adjust format string to decorate the value with a prefix, a suffix, or adapt the editing and display precision e.g. "%.3f" -> 1.234; "%5.2f secs" -> 01.23 secs; "Biscuit: % .0f" -> Biscuit: 1; etc.
        /// </summary>
        public string? Format { private get; set; }

        public ImGuiNET.ImGuiSliderFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var value = Update().ToImGui();
            if (ImGuiNET.ImGui.SliderFloat2(Context.GetLabel(this, Label), ref value, Min, Max, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                Value = value.ToVL();
        }

    }
}

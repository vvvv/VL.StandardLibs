using ImGuiNET;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (Float)", Category = "ImGui.Widgets", Tags = "number, updown")]
    [WidgetType(WidgetType.Input)]
    internal partial class InputFloat : InputWidget<float>, IHasInputTextFlags
    {
        public float Step { private get; set; } = 0.1f;

        public float StepFast { private get; set; } = 10f;

        /// <summary>
        /// Adjust format string to decorate the value with a prefix, a suffix, or adapt the editing and display precision e.g. "%.3f" -> 1.234; "%5.2f secs" -> 01.23 secs; "Biscuit: % .0f" -> Biscuit: 1; etc.
        /// </summary>
        public string? Format { private get; set; } = "%.3f";

        public ImGuiInputTextFlags Flags { get; set; }

        float lastframeValue;

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiNET.ImGui.InputFloat(widgetLabel.Update(label.Value), ref value, Step, StepFast, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                value = SetClampedValueIfChanged(value);
            lastframeValue = value;
        }

        private float SetClampedValueIfChanged(float value)
        {
            if (min.HasValue)
                value = Math.Max(value, min.Value);

            if (max.HasValue)
                value = Math.Min(value, max.Value);

            SetValueIfChanged(lastframeValue, value, Flags);
            return value;
        }
    }
}

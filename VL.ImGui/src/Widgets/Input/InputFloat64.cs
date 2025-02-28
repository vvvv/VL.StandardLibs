using ImGuiNET;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (Float64)", Category = "ImGui.Widgets.Advanced", Tags = "number, updown")]
    [WidgetType(WidgetType.Input)]
    internal partial class InputFloat64 : InputWidget<double>, IHasInputTextFlags
    {
        public double Step { private get; set; } = 0.1d;

        public double StepFast { private get; set; } = 10d;

        /// <summary>
        /// Adjust format string to decorate the value with a prefix, a suffix, or adapt the editing and display precision e.g. "%.3f" -> 1.234; "%5.2f secs" -> 01.23 secs; "Biscuit: % .0f" -> Biscuit: 1; etc.
        /// </summary>
        public string? Format { private get; set; } = "%.3f";

        public ImGuiInputTextFlags Flags { get; set; }

        double lastframeValue;

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiUtils.InputDouble(widgetLabel.Update(label.Value), ref value, Step, StepFast, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                value = SetClampedValueIfChanged(value);
            lastframeValue = value;
        }

        private double SetClampedValueIfChanged(double value)
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

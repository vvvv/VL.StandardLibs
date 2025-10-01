using ImGuiNET;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (TimeSpan)", Category = "ImGui.Widgets.Advanced")]
    [WidgetType(WidgetType.Input)]
    internal partial class InputTimeSpan : InputWidget<TimeSpan>, IHasInputTextFlags            
    {
        public InputTimeSpan()
            : base(min: TimeSpan.Zero /*should that be 0 or -inf? Do users want to specify negative timespans? */, max: TimeSpan.MaxValue)
        {

        }

        public double Step { private get; set; } = 0.1d;

        public double StepFast { private get; set; } = 10d;

        /// <summary>
        /// Adjust format string to decorate the value with a prefix, a suffix, or adapt the editing and display precision e.g. "%.3f" -> 1.234; "%5.2f secs" -> 01.23 secs; "Biscuit: % .0f" -> Biscuit: 1; etc.
        /// </summary>
        public string? Format { private get; set; } = "%.3f s";

        public ImGuiInputTextFlags Flags { get; set; }

        TimeSpan lastframeValue;

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            var seconds = value.TotalSeconds;
            if (ImGuiUtils.InputDouble(widgetLabel.Update(label.Value), ref seconds, Step, StepFast, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                SetClampedValueIfChanged(value = TimeSpan.FromSeconds(seconds));
            lastframeValue = value;
        }

        private TimeSpan SetClampedValueIfChanged(TimeSpan value)
        {
            if (min.HasValue)
                value = TimeSpan.FromTicks(Math.Max(value.Ticks, min.Value.Ticks));

            if (max.HasValue)
                value = TimeSpan.FromTicks(Math.Min(value.Ticks, max.Value.Ticks));

            SetValueIfChanged(lastframeValue, value, Flags);
            return value;
        }
    }
}

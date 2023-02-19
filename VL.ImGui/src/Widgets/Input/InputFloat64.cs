using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (Float64)", Category = "ImGui.Widgets.Advanced", Tags = "number, updown")]
    [WidgetType(WidgetType.Input)]
    internal partial class InputFloat64 : ChannelWidget<double>
    {

        public string? Label { get; set; }

        public double Step { private get; set; } = 0.1d;

        public double StepFast { private get; set; } = 10d;

        /// <summary>
        /// Adjust format string to decorate the value with a prefix, a suffix, or adapt the editing and display precision e.g. "%.3f" -> 1.234; "%5.2f secs" -> 01.23 secs; "Biscuit: % .0f" -> Biscuit: 1; etc.
        /// </summary>
        public string? Format { private get; set; } = "%.3f";

        public ImGuiNET.ImGuiInputTextFlags Flags { private get; set; }

        double lastframeValue;

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiUtils.InputDouble(Context.GetLabel(this, Label), ref value, Step, StepFast, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags) && value != lastframeValue)
                Value = value;
            lastframeValue = value;
        }
    }
}

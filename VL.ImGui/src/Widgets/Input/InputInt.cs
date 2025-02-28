using ImGuiNET;
using VL.Core;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (Int)", Category = "ImGui.Widgets", Tags = "number, updown")]
    [WidgetType(WidgetType.Input)]
    internal partial class InputInt : InputWidget<int>, IHasInputTextFlags
    {
        public int Step { private get; set; } = 1;

        public int StepFast { private get; set; } = 100;

        public ImGuiInputTextFlags Flags { get; set; }

        int lastframeValue;

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiNET.ImGui.InputInt(widgetLabel.Update(label.Value), ref value, Step, StepFast, Flags))
                value = SetClampedValueIfChanged(value);
            lastframeValue = value;
        }

        private int SetClampedValueIfChanged(int value)
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

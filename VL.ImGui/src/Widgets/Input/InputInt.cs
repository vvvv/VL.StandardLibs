using ImGuiNET;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (Int)", Category = "ImGui.Widgets", Tags = "number, updown")]
    [WidgetType(WidgetType.Input)]
    internal partial class InputInt : ChannelWidget<int>
    {

        public string? Label { get; set; }

        public int Step { private get; set; } = 1;

        public int StepFast { private get; set; } = 100;

        public ImGuiInputTextFlags Flags { private get; set; }

        int lastframeValue;

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiNET.ImGui.InputInt(Context.GetLabel(this, Label), ref value, Step, StepFast, Flags))
                SetValueIfChanged(lastframeValue, value, Flags);
            lastframeValue = value;
        }
    }
}

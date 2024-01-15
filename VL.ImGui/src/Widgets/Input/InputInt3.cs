using ImGuiNET;
using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (Int3)", Category = "ImGui.Widgets", Tags = "number, updown")]
    [WidgetType(WidgetType.Input)]
    internal partial class InputInt3 : ChannelWidget<Int3>, IHasLabel, IHasInputTextFlags
    {

        public string? Label { get; set; }

        public int Step { private get; set; } = 1;

        public int StepFast { private get; set; } = 100;

        public ImGuiInputTextFlags Flags { get; set; }

        Int3 lastframeValue;

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiUtils.InputInt3(widgetLabel.Update(Label), ref value, Flags))
                SetValueIfChanged(lastframeValue, value, Flags);
            lastframeValue = value;
        }
    }
}

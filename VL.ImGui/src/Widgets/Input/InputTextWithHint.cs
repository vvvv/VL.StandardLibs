using ImGuiNET;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (String Hint)", Category = "ImGui.Widgets", Tags = "edit, textfield")]
    internal partial class InputTextWithHint : ChannelWidget<string>, IHasLabel, IHasInputTextFlags
    {

        public string? Label { get; set; }

        public string? Hint { get; set; }

        public int MaxLength { get; set; } = 100;

        public ImGuiInputTextFlags Flags { get; set; }

        string? lastframeValue;

        internal override void UpdateCore(Context context)
        {
            var value = Update() ?? string.Empty;
            if (ImGuiNET.ImGui.InputTextWithHint(widgetLabel.Update(Label), Hint ?? string.Empty, ref value, (uint)MaxLength, Flags))
                SetValueIfChanged(lastframeValue, value, Flags);
            lastframeValue = value;
        }
    }
}

using VL.Core.EditorAttributes;
using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (String)", Category = "ImGui.Widgets", Tags = "edit, textfield")]
    [WidgetType(WidgetType.Input)]
    internal partial class InputText : ChannelWidget<string>
    {

        public string? Label { get; set; }

        public int MaxLength { get; set; } = 100;

        public ImGuiNET.ImGuiInputTextFlags Flags { private get; set; }

        string? lastframeValue;

        internal override void UpdateCore(Context context)
        {
            var value = Update() ?? string.Empty;
            if (ImGuiNET.ImGui.InputText(Context.GetLabel(this, Label), ref value, (uint)MaxLength, Flags) && value != lastframeValue)
                Value = value;
            lastframeValue = value;
        }
    }
}

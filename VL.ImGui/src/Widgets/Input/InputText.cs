using ImGuiNET;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (String)", Category = "ImGui.Widgets", Tags = "edit, textfield")]
    [WidgetType(WidgetType.Input)]
    internal partial class InputText : ChannelWidget<string>, IHasLabel, IHasInputTextFlags
    {

        public string? Label { get; set; }

        public int MaxLength { get; set; } = 100;

        public ImGuiInputTextFlags Flags { get; set; }

        string? lastframeValue;

        internal override void UpdateCore(Context context)
        {
            var value = Update() ?? string.Empty;
            if (ImGuiNET.ImGui.InputText(Context.GetLabel(this, Label), ref value, (uint)MaxLength, Flags))
                SetValueIfChanged(lastframeValue, value, Flags);
            lastframeValue = value;
        }
    }
}

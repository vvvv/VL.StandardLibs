using ImGuiNET;
using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (String Multiline)", Category = "ImGui.Widgets", Tags = "edit, textfield")]
    [WidgetType(WidgetType.Multiline)]
    internal partial class InputTextMultiline : ChannelWidget<string>, IHasLabel, IHasInputTextFlags
    {

        public string? Label { get; set; }

        public int MaxLength { get; set; } = 100;

        public Vector2 Size { get; set; }

        public ImGuiInputTextFlags Flags { get; set; }

        string? lastframeValue;

        internal override void UpdateCore(Context context)
        {
            var value = Update() ?? string.Empty;
            if (ImGuiNET.ImGui.InputTextMultiline(widgetLabel.Update(Label), ref value, (uint)MaxLength, Size.FromHectoToImGui(), Flags))
                SetValueIfChanged(lastframeValue, value, Flags);
            lastframeValue = value;
        }
    }
}

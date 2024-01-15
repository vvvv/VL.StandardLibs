using ImGuiNET;
using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (Vector4)", Category = "ImGui.Widgets", Tags = "number, updown")]
    [WidgetType(WidgetType.Input)]
    internal partial class InputVector4 : ChannelWidget<Vector4>, IHasLabel, IHasInputTextFlags
    {

        public string? Label { get; set; }

        /// <summary>
        /// Adjust format string to decorate the value with a prefix, a suffix, or adapt the editing and display precision e.g. "%.3f" -> 1.234; "%5.2f secs" -> 01.23 secs; "Biscuit: % .0f" -> Biscuit: 1; etc.
        /// </summary>
        public string? Format { private get; set; } = "%.3f";

        public ImGuiInputTextFlags Flags { get; set; }

        Vector4 lastframeValue;

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiUtils.InputFloat4(widgetLabel.Update(Label), ref value, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                SetValueIfChanged(lastframeValue, value, Flags);
            lastframeValue = value;
        }
    }
}

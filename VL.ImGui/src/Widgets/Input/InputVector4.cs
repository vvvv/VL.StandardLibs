using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (Vector4)", Category = "ImGui.Widgets", Tags = "number, updown")]
    [WidgetType(WidgetType.Input)]
    internal partial class InputVector4 : ChannelWidget<Vector4>
    {

        public string? Label { get; set; }

        /// <summary>
        /// Adjust format string to decorate the value with a prefix, a suffix, or adapt the editing and display precision e.g. "%.3f" -> 1.234; "%5.2f secs" -> 01.23 secs; "Biscuit: % .0f" -> Biscuit: 1; etc.
        /// </summary>
        public string? Format { private get; set; } = "%.3f";

        public ImGuiNET.ImGuiInputTextFlags Flags { private get; set; }

        System.Numerics.Vector4 lastframeValue;

        internal override void UpdateCore(Context context)
        {
            var value = Update().ToImGui();
            if (ImGuiNET.ImGui.InputFloat4(Context.GetLabel(this, Label), ref value, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags) && value != lastframeValue)
                Value = value.ToVL();
            lastframeValue = value;
        }
    }
}

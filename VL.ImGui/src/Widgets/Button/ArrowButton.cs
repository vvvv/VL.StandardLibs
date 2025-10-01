using System.Reactive;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Square button with an arrow shape
    /// </summary>
    [GenerateNode(Category = "ImGui.Widgets", Button = true, Tags = "bang")]
    internal partial class ArrowButton : ChannelWidget<Unit>
    {
        public ImGuiNET.ImGuiDir Direction { private get; set; } = ImGuiNET.ImGuiDir.None;

        internal override void UpdateCore(Context context)
        {
            Update();
            if (ImGuiNET.ImGui.ArrowButton(widgetLabel.Update(label.Value), Direction))
                Value = Unit.Default;

            widgetLabel.DrawLabelInSameLine();
        }
    }
}

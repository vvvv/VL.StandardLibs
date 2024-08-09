using System.Reactive;
using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Square button with an arrow shape
    /// </summary>
    [GenerateNode(Category = "ImGui.Widgets", Button = true, Tags = "bang")]
    internal partial class ArrowButton : ChannelWidget<Bang>
    {
        public ImGuiNET.ImGuiDir Direction { private get; set; }

        internal override void UpdateCore(Context context)
        {
            Update();
            if (ImGuiNET.ImGui.ArrowButton(widgetLabel.Update(label.Value), Direction))
                Value = Lib.Reactive.Bang.Trigger;
        }
    }
}

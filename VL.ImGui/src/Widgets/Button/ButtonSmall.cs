using System.Reactive;
using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Button with FramePadding=(0,0) to easily embed within text
    /// </summary>
    [GenerateNode(Name ="Button (Small)", Category = "ImGui.Widgets", Button = true, Tags = "bang")]
    internal partial class ButtonSmall : ChannelWidget<Bang>
    {
        internal override void UpdateCore(Context context)
        {
            Update();
            if (ImGuiNET.ImGui.SmallButton(widgetLabel.Update(label.Value)))
                Value = Lib.Reactive.Bang.Trigger;
        }
    }
}

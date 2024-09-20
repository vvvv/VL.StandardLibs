using System.Reactive;
using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{

    [GenerateNode(Category = "ImGui.Widgets", Button = true)]
    internal partial class TabItemButton : ChannelWidget<Bang>
    {
        public ImGuiNET.ImGuiTabItemFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            Update();
            if (ImGuiNET.ImGui.TabItemButton(widgetLabel.Update(label.Value), Flags))
                Value = Lib.Reactive.Bang.Trigger;
        }
    }
}

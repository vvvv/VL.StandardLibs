using VL.Lib.Reactive;
using System.Reactive;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Create a MenuItem. Keyboardshortcuts are displayed as a convenience but not processed by Dear ImGui at the moment.
    /// </summary>
    [GenerateNode(Category = "ImGui.Widgets", Button = true)]
    internal partial class MenuItem : ChannelWidget<Unit>, IHasLabel
    {
        public string? Label { get; set; }

        public string? Shortcut { get; set; }

        public bool Enabled { get; set; } = true;

        public bool Selectable { get; set; }

        public bool IsSelected => IsSelectedFlange.Value;

        [Pin(DefaultValue = "false")]
        public IChannel<bool>? SelectedChannel { private get; set; }
        ChannelFlange<bool> IsSelectedFlange = new ChannelFlange<bool>(true);

        internal override void UpdateCore(Context context)
        {
            var value = Update();

            if (Selectable)
            {
                var isSelected = IsSelectedFlange.Update(SelectedChannel);
                if (ImGuiNET.ImGui.MenuItem(widgetLabel.Update(Label), Shortcut, ref isSelected, enabled: Enabled))
                    Value = Unit.Default;
                IsSelectedFlange.Value = isSelected;
            }
            else
            if (ImGuiNET.ImGui.MenuItem(widgetLabel.Update(Label), Shortcut, selected: false, enabled: Enabled))
                Value = Unit.Default;

        }
    }
}

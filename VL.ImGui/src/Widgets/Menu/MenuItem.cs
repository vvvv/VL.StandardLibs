namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Create a MenuItem. Keyboardshortcuts are displayed as a convenience but not processed by Dear ImGui at the moment.
    /// </summary>
    [GenerateNode(Category = "ImGui.Widgets")]
    internal partial class MenuItem : ChannelWidget<bool>
    {

        public string? Label { get; set; }

        public string? Shortcut { get; set; }

        public bool Enabled { get; set; } = true;

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiNET.ImGui.MenuItem(Context.GetLabel(this, Label), Shortcut, ref value, Enabled))
                Value = value;
        }
    }
}

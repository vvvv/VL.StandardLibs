namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Set popup state to open. See Flags for available opening options.
    /// Don't call every frame!
    ///  If not modal: they can be closed by clicking anywhere outside them, or by pressing ESCAPE.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class OpenPopup : Widget
    {
        public string? Label { private get; set; }

        public ImGuiNET.ImGuiPopupFlags Flags { private get; set; }

        public bool Enabled { private get; set; } = true;

        internal override void UpdateCore(Context context)
        {
            if (Label != null && Enabled)
               ImGuiNET.ImGui.OpenPopup(Label, Flags);
        }
    }
}

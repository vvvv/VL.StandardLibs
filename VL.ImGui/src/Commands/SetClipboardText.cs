namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Capture a text into clipboard.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SetClipboardText : Widget
    {

        public string? Text { private get; set; }

        public bool Enabled { private get; set; } = true;

        internal override void UpdateCore(Context context)
        {
            if (Enabled)
                ImGuiNET.ImGui.SetClipboardText(Text ?? string.Empty);
        }
    }
}

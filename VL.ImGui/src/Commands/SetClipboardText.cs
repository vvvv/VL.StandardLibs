namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    /// <summary>
    /// Capture a text into clipboard.
    /// </summary>
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

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Retrieve text data from the clipboard
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class GetClipboardText : ChannelWidget<string>
    {

        public bool Enabled { private get; set; } = true;

        protected override void UpdateCore(Context context)
        {
            if (Enabled)
                Value = ImGuiNET.ImGui.GetClipboardText();
        }
    }
}

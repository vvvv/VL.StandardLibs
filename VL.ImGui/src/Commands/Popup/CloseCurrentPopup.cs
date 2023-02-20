namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Use inside the Popup Region to close it manually.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class CloseCurrentPopup : Widget
    {
        public bool Enabled { private get; set; } = true;

        internal override void UpdateCore(Context context)
        {
            if (Enabled)
               ImGuiNET.ImGui.CloseCurrentPopup();
        }
    }
}

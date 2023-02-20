namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Focus keyboard on the next widget. Use positive 'offset' to access sub components of a multiple component widget. Use -1 to access previous widget.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SetKeyboardFocusHere : Widget
    {
        public int Offset { private get; set; }

        public bool Enabled { private get; set; } = true;

        internal override void UpdateCore(Context context)
        {
            if (Enabled)
                ImGuiNET.ImGui.SetKeyboardFocusHere(Offset);
        }
    }
}

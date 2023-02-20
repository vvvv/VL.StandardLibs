namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Make last item the default focused item of a window.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SetItemDefaultFocus : Widget
    {

        public bool Enabled { private get; set; } = true;

        internal override void UpdateCore(Context context)
        {
            if (Enabled)
                ImGuiNET.ImGui.SetItemDefaultFocus();
        }
    }
}

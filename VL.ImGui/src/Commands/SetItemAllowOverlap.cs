namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Allow last item to be overlapped by a subsequent item. Sometimes useful with invisible buttons, selectables, etc. to catch unused area.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SetItemAllowOverlap : Widget
    {

        internal override void UpdateCore(Context context)
        {
                ImGuiNET.ImGui.SetItemAllowOverlap();
        }
    }
}

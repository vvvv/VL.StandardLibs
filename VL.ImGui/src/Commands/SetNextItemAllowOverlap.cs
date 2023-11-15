namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Allow next item to be overlapped by a subsequent item. Useful with invisible buttons, selectable, treenode covering an area where subsequent items may need to be added. Note that both Selectable() and TreeNode() have dedicated flags doing this.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SetNextItemAllowOverlap : Widget
    {

        internal override void UpdateCore(Context context)
        {
                ImGuiNET.ImGui.SetNextItemAllowOverlap();
        }
    }
}

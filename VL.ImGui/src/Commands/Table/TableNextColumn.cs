namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    /// <summary>
    /// Append into the next column (or first column of next row if currently in last column). Return true when column is visible.
    /// </summary>
    internal partial class TableNextColumn : Widget
    {
        public bool IsVisible { get; private set; }

        internal override void UpdateCore(Context context)
        {
            IsVisible = ImGuiNET.ImGui.TableNextColumn();
        }
    }
}
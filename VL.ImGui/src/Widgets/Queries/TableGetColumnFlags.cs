namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Return column flags so you can query their Enabled/Visible/Sorted/Hovered status flags.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class TableGetColumnFlags : Query
    {
        /// <summary>
        /// Pass -1 to use current column.
        /// </summary>
        public int Index { private get; set; } = -1;

        public ImGuiNET.ImGuiTableColumnFlags Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.TableGetColumnFlags(Index);
        }
    }
}

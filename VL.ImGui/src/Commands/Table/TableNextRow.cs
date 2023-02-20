namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Append into the first cell of a new row.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class TableNextRow : Widget
    {

        public float MinimumRowHeight { private get; set; } = 0f;

        public ImGuiNET.ImGuiTableRowFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.TableNextRow(Flags, MinimumRowHeight);
        }
    }
}
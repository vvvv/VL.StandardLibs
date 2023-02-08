namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    /// <summary>
    /// Append into the first cell of a new row.
    /// </summary>
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
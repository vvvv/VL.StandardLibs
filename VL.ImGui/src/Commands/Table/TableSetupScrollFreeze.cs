namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Lock columns/rows so they stay visible when scrolled.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class TableSetupScrollFreeze : Widget
    {
        public int NumberOfColumns { private get; set; }
        public int NumberOfRows { private get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.TableSetupScrollFreeze(NumberOfColumns, NumberOfRows);
        }
    }
}
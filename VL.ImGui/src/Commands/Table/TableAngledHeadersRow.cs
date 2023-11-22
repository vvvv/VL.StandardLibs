namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Submit a row with angled headers for every column with the ImGuiTableColumnFlags_AngledHeader flag. MUST BE FIRST ROW.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class TableAngledHeadersRow : Widget
    {

        internal override void UpdateCore(Context context)
        {
            if (context.IsInBeginTables)
                ImGuiNET.ImGui.TableAngledHeadersRow();
        }
    }
}
namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Submit all headers cells based on data provided to TableSetupColumn() + submit context menu
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class TableHeadersRow : Widget
    {

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.TableHeadersRow();
        }
    }
}
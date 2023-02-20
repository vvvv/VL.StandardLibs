namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Change user accessible enabled/disabled state of a column. Set to false to hide the column. User can use the context menu to change this themselves (right-click in headers, or right-click in columns body with ImGuiTableFlags_ContextMenuInBody)
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class TableSetColumnEnabled : Widget
    {

        public int Index { private get; set; }

        public bool IsVisible { private get; set; } = true;

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.TableSetColumnEnabled(Index, IsVisible);
        }
    }
}
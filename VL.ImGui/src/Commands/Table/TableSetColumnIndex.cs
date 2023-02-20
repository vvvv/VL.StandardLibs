namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Append into the specified column. Return true when column is visible.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class TableSetColumnIndex : Widget
    {

        public int Index { private get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.TableSetColumnIndex(Index);
        }
    }
}
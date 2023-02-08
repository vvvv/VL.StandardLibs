namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    /// <summary>
    /// Append into the specified column. Return true when column is visible.
    /// </summary>
    internal partial class TableSetColumnIndex : Widget
    {

        public int Index { private get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.TableSetColumnIndex(Index);
        }
    }
}
namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Return current row index.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class TableGetRowIndex : Query
    {
        public int Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.TableGetRowIndex();
        }
    }
}

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Return number of columns (value passed to BeginTable)
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class TableGetColumnCount : Query
    {
        public int Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.TableGetColumnCount();
        }
    }
}

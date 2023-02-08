namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Return current column index.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class TableGetColumnIndex : Widget
    {
        public int Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.TableGetColumnIndex();
        }
    }
}

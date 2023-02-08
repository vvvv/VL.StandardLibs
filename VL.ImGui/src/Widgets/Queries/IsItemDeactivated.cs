namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Was the last item just made inactive (item was previously active). Useful for Undo/Redo patterns with widgets that requires continuous editing.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsItemDeactivated : Widget
    {

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsItemDeactivated();
        }
    }
}

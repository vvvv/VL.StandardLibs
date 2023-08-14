namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Was the last item just made inactive (item was previously active). Useful for Undo/Redo patterns with widgets that requires continuous editing.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsItemDeactivated : Query
    {

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var capturedState = context.CapturedItemState;
            if (capturedState.HasValue)
                Value = capturedState.Value.IsDeactived;
            else
                Value = ImGuiNET.ImGui.IsItemDeactivated();
        }
    }
}

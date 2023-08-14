namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Was the last item open state toggled? set by TreeNode.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsItemToggledOpen : Query
    {

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var capturedState = context.CapturedItemState;
            if (capturedState.HasValue)
                Value = capturedState.Value.IsToggledOpen;
            else
                Value = ImGuiNET.ImGui.IsItemToggledOpen();
        }
    }
}

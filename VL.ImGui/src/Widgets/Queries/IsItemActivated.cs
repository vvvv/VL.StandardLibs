namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Was the last item just made active (item was previously inactive).
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsItemActivated : Query
    {

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var capturedState = context.CapturedItemState;
            if (capturedState.HasValue)
                Value = capturedState.Value.IsActivated;
            else
                Value = ImGuiNET.ImGui.IsItemActivated();
        }
    }
}

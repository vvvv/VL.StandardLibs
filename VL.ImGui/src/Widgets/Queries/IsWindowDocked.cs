namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is current window docked into another window?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsWindowDocked : Query
    {

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var capturedState = context.CapturedItemState;
            if (capturedState.HasValue)
                Value = capturedState.Value.IsEdited;
            else
                Value = ImGuiNET.ImGui.IsWindowDocked();
        }
    }
}

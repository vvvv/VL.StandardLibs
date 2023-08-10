namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is the last item focused for keyboard/gamepad navigation?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsItemFocused : Query
    {

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var capturedState = context.CapturedItemState;
            if (capturedState.HasValue)
                Value = capturedState.Value.IsFocused;
            else
                Value = ImGuiNET.ImGui.IsItemFocused();
        }
    }
}

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is the last item focused for keyboard/gamepad navigation?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsItemFocused : Widget
    {

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsItemFocused();
        }
    }
}

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is the last item active? (e.g. button being held, text field being edited. This will continuously return true while holding mouse button on an item. Items that don't interact will always return false)
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsItemActive : Widget
    {

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsItemActive();
        }
    }
}

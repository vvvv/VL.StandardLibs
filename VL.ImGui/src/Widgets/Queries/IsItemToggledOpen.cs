namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Was the last item open state toggled? set by TreeNode.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsItemToggledOpen : Widget
    {

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsItemToggledOpen();
        }
    }
}

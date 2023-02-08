namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    /// <summary>
    /// Set next TreeNode/CollapsingHeader open state.
    /// </summary>
    internal partial class SetNextItemOpen : Widget
    {

        public bool IsOpen { private get; set; }
        public ImGuiNET.ImGuiCond Condition { private get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.SetNextItemOpen(IsOpen, Condition);
        }
    }
}

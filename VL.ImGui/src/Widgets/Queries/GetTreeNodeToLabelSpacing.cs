namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Horizontal distance preceding label when using TreeNode or Bullet == (g.FontSize + style.FramePadding.x*2) for a regular unframed TreeNode
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetTreeNodeToLabelSpacing : Query
    {

        public float Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.GetTreeNodeToLabelSpacing().ToVLHecto();
        }
    }
}

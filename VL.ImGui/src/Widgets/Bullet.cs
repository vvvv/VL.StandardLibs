namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Draw a small circle + keep the cursor on the same line. Advance cursor x position by GetTreeNodeToLabelSpacing(), same distance that TreeNode() uses
    /// </summary>
    [GenerateNode(Category = "ImGui.Widgets")]
    internal partial class Bullet : Widget
    {

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.Bullet();
        }
    }
}

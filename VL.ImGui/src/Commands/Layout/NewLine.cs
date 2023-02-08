namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Undo a SameLine or force a new line when in an horizontal-layout context.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class NewLine : Widget
    {

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.NewLine();
        }
    }
}

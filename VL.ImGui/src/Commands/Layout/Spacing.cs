namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Add vertical spacing.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class Spacing : Widget
    {

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.Spacing();
        }
    }
}

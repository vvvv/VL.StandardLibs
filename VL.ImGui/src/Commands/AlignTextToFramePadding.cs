namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", IsStylable = false)]
    internal partial class AlignTextToFramePadding : Widget
    {

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.AlignTextToFramePadding();
        }
    }
}

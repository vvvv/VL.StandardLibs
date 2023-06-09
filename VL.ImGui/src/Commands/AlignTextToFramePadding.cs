namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", IsStylable = false)]
    internal partial class AlignTextToFramePadding : Widget
    {

        protected override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.AlignTextToFramePadding();
        }
    }
}

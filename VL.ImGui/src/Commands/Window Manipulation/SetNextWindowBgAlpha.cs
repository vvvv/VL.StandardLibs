namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SetNextWindowBgAlpha : Widget
    {
        public float Alpha { private get; set; } = 1f;

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.SetNextWindowBgAlpha(Alpha);
        }
    }
}

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class ResetMouseDragDelta : Widget
    {

        public bool Enabled { private get; set; } = true;

        protected override void UpdateCore(Context context)
        {
            if (Enabled)
                ImGuiNET.ImGui.ResetMouseDragDelta();
        }
    }
}

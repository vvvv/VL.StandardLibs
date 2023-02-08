namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Call between widgets or groups to layout them horizontally. X position given in window coordinates.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SameLine : Widget
    {

        public float Offset { private get; set; }

        public float Spacing { private get; set; } = -1f;

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.SameLine(Offset.FromHectoToImGui(), Spacing.FromHectoToImGui());
        }
    }
}

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Set width of the next common large "item+label" widget. &gt;0.0: width in pixels, &lt;0.0 align xx pixels to the right of window.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SetNextItemWidth : Widget
    {
        public float Width { private get; set; } = 1f;

        internal override void UpdateCore(Context context)
        {
                ImGuiNET.ImGui.SetNextItemWidth(Width.FromHectoToImGui());
        }
    }
}

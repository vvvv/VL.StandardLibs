namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Push width of items for common large "item+label" widgets. &gt;0.0f: width in pixels, &lt;0.0f align xx pixels to the right of window (so -FLT_MIN always align width to the right side).
    /// </summary>
    [GenerateNode(Category = "ImGui.Widgets", GenerateImmediate = false, IsStylable = false)]
    internal partial class SetItemWidth : Widget
    {
        public Widget? Content { private get; set; }

        public float Width { private get; set; } = 1f;

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.PushItemWidth(Width.FromHectoToImGui());
            try
            {
                context.Update(Content);
            }
            finally
            {
                ImGuiNET.ImGui.PopItemWidth();
            }
        }
    }
}

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is the last item hovered? (and usable, aka not blocked by a popup, etc.). See ImGuiHoveredFlags for more options.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsItemHovered : Widget
    {

        public ImGuiNET.ImGuiHoveredFlags Flags { private get; set; }

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsItemHovered(Flags);
        }
    }
}

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is current window hovered (and typically: not blocked by a popup/modal)? See flags for options.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", GenerateRetained = false)]
    internal partial class IsWindowHovered : Query
    {

        public ImGuiNET.ImGuiHoveredFlags Flags { set; private get; }

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsWindowHovered(Flags);
        }
    }
}
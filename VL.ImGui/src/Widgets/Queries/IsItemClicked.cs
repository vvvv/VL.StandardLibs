namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is the last item hovered and mouse clicked on?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsItemClicked : Widget
    {

        public ImGuiNET.ImGuiMouseButton Flags { private get; set; }

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsItemClicked(Flags);
        }
    }
}
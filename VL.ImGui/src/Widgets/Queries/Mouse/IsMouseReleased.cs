namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Did mouse button released?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class IsMouseReleased : Query
    {

        public ImGuiNET.ImGuiMouseButton Flags { private get; set; }

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsMouseReleased(Flags);
        }
    }
}

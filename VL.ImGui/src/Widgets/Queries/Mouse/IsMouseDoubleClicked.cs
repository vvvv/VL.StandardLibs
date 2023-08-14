namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Did mouse button double-clicked?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class IsMouseDoubleClicked : Query
    {

        public ImGuiNET.ImGuiMouseButton Flags { private get; set; }

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsMouseDoubleClicked(Flags);
        }
    }
}

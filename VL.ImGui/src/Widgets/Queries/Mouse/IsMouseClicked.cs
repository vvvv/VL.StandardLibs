namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Did mouse button clicked?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class IsMouseClicked : Query
    {

        public ImGuiNET.ImGuiMouseButton Flags { private get; set; }

        public bool Repeat { private get; set; } = true;

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsMouseClicked(Flags, Repeat);
        }
    }
}

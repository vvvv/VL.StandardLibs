namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is mouse button held?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class IsMouseDown : Query
    {

        public ImGuiNET.ImGuiMouseButton Flags { private get; set; }

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsMouseDown(Flags);
        }
    }
}

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Get a string corresponding to the enum value (for display, saving, etc.).
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetStyleColorName : Query
    {
        public ImGuiNET.ImGuiCol Color { private get; set; }

        public string? Value { get; private set; }

        internal override unsafe void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.GetStyleColorName(Color);
        }
    }
}

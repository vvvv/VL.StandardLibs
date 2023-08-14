namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Return primary/default viewport.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetMainViewport : Query
    {
        public ImGuiNET.ImGuiViewportPtr Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.GetMainViewport();
        }
    }
}

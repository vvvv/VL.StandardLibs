namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is current window focused? Or its root/child, depending on flags.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", GenerateRetained = false)]
    internal partial class IsWindowFocused : Query
    {

        public ImGuiNET.ImGuiFocusedFlags Flags { set; private get; }

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsWindowFocused(Flags);
        }
    }
}
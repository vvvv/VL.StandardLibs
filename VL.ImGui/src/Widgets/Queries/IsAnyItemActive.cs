namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is any item active?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsAnyItemActive : Query
    {

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsAnyItemActive();
        }
    }
}

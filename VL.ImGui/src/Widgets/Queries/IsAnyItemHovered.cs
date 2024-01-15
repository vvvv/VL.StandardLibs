namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is any item hovered?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsAnyItemHovered : Query
    {

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsAnyItemHovered();
        }
    }
}

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Distance in pixels between 2 consecutive lines of text. Approx. FontSize + style.ItemSpacing.y
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetTextLineHeightWithSpacing : Query
    {

        public float Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.GetTextLineHeightWithSpacing().ToVLHecto();
        }
    }
}

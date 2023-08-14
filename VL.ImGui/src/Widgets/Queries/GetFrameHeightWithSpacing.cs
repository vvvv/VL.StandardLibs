namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Distance in pixels between 2 consecutive lines of framed widgets. Approx. FontSize + style.FramePadding.y * 2 + style.ItemSpacing.y
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetFrameHeightWithSpacing : Query
    {

        public float Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.GetFrameHeightWithSpacing().ToVLHecto();
        }
    }
}

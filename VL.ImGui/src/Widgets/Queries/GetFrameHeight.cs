namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Approx. FontSize + style.FramePadding.y * 2
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetFrameHeight : Query
    {

        public float Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.GetFrameHeight().ToVLHecto();
        }
    }
}

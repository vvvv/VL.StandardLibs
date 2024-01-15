namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Approx. = FontSize
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetTextLineHeight : Query
    {

        public float Value { get; private set; }


        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.GetTextLineHeight().ToVLHecto();
        }
    }
}

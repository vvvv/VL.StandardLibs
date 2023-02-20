namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Initialize current options (generally on application startup) if you want to select a default format, picker type, etc.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SetColorEditOptions : Widget
    {
        public ImGuiNET.ImGuiColorEditFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.SetColorEditOptions(Flags);
        }
    }
}
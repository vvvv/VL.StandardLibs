namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Text (Wrapped)", Category = "ImGui.Widgets", Tags = "label")]
    internal partial class TextWrapped : Widget
    {
        public string? Text { private get; set; } = "[Text]";

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.TextWrapped(Text ?? String.Empty);
        }
    }
}

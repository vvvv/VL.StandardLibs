namespace VL.ImGui.Widgets
{
    using ImGui = ImGuiNET.ImGui;

    [GenerateNode(Name = "Text (Wrapped)", Category = "ImGui.Widgets", Tags = "label")]
    internal partial class TextWrapped : Widget
    {
        public string? Text { private get; set; } = "[Text]";

        internal override void UpdateCore(Context context)
        {
            // We don't want to use TextWrapped because of its length limitations
            // https://github.com/ocornut/imgui/blob/204cb4d226951f98dc8bfc13c7bccff9995e2690/imgui.h#L490
            ImGui.PushTextWrapPos(0f);
            ImGui.TextUnformatted(Text ?? string.Empty);
            ImGui.PopTextWrapPos();
        }
    }
}

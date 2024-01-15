namespace VL.ImGui.Widgets
{
    using ImGui = ImGuiNET.ImGui;

    [GenerateNode(Name = "Text", Category = "ImGui.Widgets", Tags = "label")]
    internal partial class TextWidget : Widget
    {
        public string? Text { private get; set; } = "[Text]";

        public bool Disabled { private get; set; } = false;

        internal override void UpdateCore(Context context)
        {
            if (!Disabled)
            {
                ImGui.TextUnformatted(Text ?? string.Empty);
            }
            else
            {
                // We don't want to use Text because of its length limitations
                // https://github.com/ocornut/imgui/blob/204cb4d226951f98dc8bfc13c7bccff9995e2690/imgui.h#L488
                var style = ImGui.GetStyle();
                ImGui.PushStyleColor(ImGuiNET.ImGuiCol.Text, style.Colors[(int)ImGuiNET.ImGuiCol.TextDisabled]);
                ImGui.TextUnformatted(Text ?? string.Empty);
                ImGui.PopStyleColor();
            }
        }
    }
}

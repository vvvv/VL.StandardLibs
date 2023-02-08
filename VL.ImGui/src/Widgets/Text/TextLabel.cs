namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Text (Label Value)", Category = "ImGui.Widgets")]
    internal partial class TextLabel : Widget
    {
        public string? Label { private get; set; } = "[Label]";
        public string? Value { private get; set; } = "[Value]";

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.LabelText(Context.GetLabel(this, Label), Value ?? String.Empty);
        }
    }
}

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", GenerateImmediate = false)]
    internal partial class TableSetupColumn : Widget
    {

        public string? Label { get; set; }

        public ImGuiNET.ImGuiTableColumnFlags Flags { private get; set; }

        public float InitWidth { get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.TableSetupColumn(Context.GetLabel(this, Label), Flags, InitWidth);
        }
    }
}

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
            // TODO: Check table resize policy
            var flags = Flags;
            if (InitWidth > 0f && (flags & ImGuiNET.ImGuiTableColumnFlags.WidthMask) == 0)
                flags |= ImGuiNET.ImGuiTableColumnFlags.WidthFixed;
            ImGuiNET.ImGui.TableSetupColumn(widgetLabel.Update(Label), flags, InitWidth);
        }
    }
}

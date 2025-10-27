﻿namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", Name = "TableSetupColumn", GenerateRetained = false, IsStylable = false)]
    internal partial class TableSetupColumnImmediate : Widget
    {

        public string? Label { get; set; }

        public float InitWidth { get; set; }

        public ImGuiNET.ImGuiTableColumnFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            // TODO: Check table resize policy
            var flags = Flags;
            if (InitWidth > 0f && (flags & ImGuiNET.ImGuiTableColumnFlags.WidthMask) == 0)
                flags |= ImGuiNET.ImGuiTableColumnFlags.WidthFixed;
            if (context.IsInBeginTables)
                ImGuiNET.ImGui.TableSetupColumn(widgetLabel.Update(Label), flags, InitWidth.FromHectoToImGui());
        }
    }
}
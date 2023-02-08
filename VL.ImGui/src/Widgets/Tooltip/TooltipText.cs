using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using VL.Core;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", Name = "Tooltip (String)")]
    internal partial class TooltipText : Widget
    {
        public string? Content { private get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.SetTooltip(Content ?? string.Empty);
        }
    }
}

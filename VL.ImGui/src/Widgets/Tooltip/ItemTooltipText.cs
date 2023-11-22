using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using VL.Core;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", Name = "ItemTooltip (String)")]
    internal partial class ItemTooltipText : Widget
    {
        public string? Content { private get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.SetItemTooltip(Content ?? string.Empty);
        }
    }
}

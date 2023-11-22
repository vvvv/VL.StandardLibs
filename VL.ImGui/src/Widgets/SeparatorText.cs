using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using VL.Core;

namespace VL.ImGui.Widgets
{

    /// <summary>
    /// Formatted text with an horizontal line
    /// </summary>
    [GenerateNode(Name = "Separator (Text)", Category = "ImGui.Widgets")]
    internal partial class SeparatorText : Widget
    {
        public string Label { private get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.SeparatorText(Label ?? string.Empty);
        }
    }
}

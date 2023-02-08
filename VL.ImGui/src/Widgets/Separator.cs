using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using VL.Core;

namespace VL.ImGui.Widgets
{

    /// <summary>
    /// Separator, generally horizontal. Inside a menu bar or in horizontal layout mode, this becomes a vertical separator.
    /// </summary>
    [GenerateNode(Category = "ImGui.Widgets")]
    internal partial class Separator: Widget
    {
        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.Separator();
        }
    }
}

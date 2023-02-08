using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using VL.Core;
using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets")]
    internal partial class StyleEditor : Widget
    {

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.ShowStyleEditor();
        }
    }
}

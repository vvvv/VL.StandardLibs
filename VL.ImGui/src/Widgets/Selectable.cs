using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using VL.Core;
using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// A selectable highlights when hovered, and can display another color when selected. Neighbors selectable extend their highlight bounds in order to leave no gap between them. This is so a series of selected Selectable appear contiguous.
    /// </summary>
    [GenerateNode(Category = "ImGui.Widgets")]
    internal partial class Selectable : ChannelWidget<bool>
    {

        public string? Label { get; set; }

        public Vector2 Size { private get; set; }

        public ImGuiNET.ImGuiSelectableFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiNET.ImGui.Selectable(Context.GetLabel(this, Label), ref value, Flags, Size.FromHectoToImGui()))
                Value = value;
        }
    }
}

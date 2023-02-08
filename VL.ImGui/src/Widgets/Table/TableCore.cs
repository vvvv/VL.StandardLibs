using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using VL.Core;
using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", GenerateImmediate = false)]
    internal sealed partial class TableCore : Widget
    {
        public int Count { get; set; }

        public Widget? Content { get; set; } 

        public string? Label { get; set; }

        public Vector2 Size { private get; set; }

        public float InnerWidth { private get; set; }

        public ImGuiNET.ImGuiTableFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            if (ImGuiNET.ImGui.BeginTable(Context.GetLabel(this, Label), Math.Max(1, Count), Flags, Size.FromHectoToImGui(), InnerWidth))
            {
                try
                {
                    context?.Update(Content);
                }
                finally
                {
                    ImGuiNET.ImGui.EndTable();
                }
            }
        }
    }
}

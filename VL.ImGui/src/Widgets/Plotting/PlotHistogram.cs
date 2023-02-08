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
    internal partial class PlotHistogram : Widget
    {

        public string? Label { get; set; }

        public IEnumerable<float> Values { get; set; } = Enumerable.Empty<float>();

        public int Offset { get; set; }

        public string OverlayText { get; set; } = String.Empty;

        public float ScaleMin { get; set; } = -1.0f;

        public float ScaleMax { get; set; } = 1.0f;

        public Vector2 Size { get; set; }

        internal override void UpdateCore(Context context)
        {
            var values = Values.ToArray();
            ImGuiNET.ImGui.PlotHistogram(Context.GetLabel(this, Label), ref values[0], values.Count(), Offset, OverlayText, ScaleMin, ScaleMax, Size.FromHectoToImGui());
        }
    }
}

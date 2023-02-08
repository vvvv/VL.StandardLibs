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
    internal partial class ProgressBar : Widget
    {

        public string? Label { get; set; }

        public float Fraction { get; set; }

        public string OverlayText { get; set; } = String.Empty;

        public Vector2 Size { get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.ProgressBar(Fraction, Size.FromHectoToImGui(), OverlayText);        }
    }
}

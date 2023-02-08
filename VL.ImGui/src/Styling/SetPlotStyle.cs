using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using ImGuiNET;

namespace VL.ImGui.Styling
{
    using ImGui = ImGuiNET.ImGui;

    // We decided that the style nodes shall take all the relevant values in one go (= disable fragments).

    [GenerateNode(Fragmented = false, Category = "ImGui.Styling", GenerateImmediate = false,
        Tags = "PlotLines PlotLinesHovered PlotHistogram PlotHistogramHovered")]
    internal partial class SetPlotStyle : StyleBase
    {
        public Optional<Color4> Line { private get; set; }

        public Optional<Color4> LineHovered { private get; set; }

        public Optional<Color4> Histogram { private get; set; }

        public Optional<Color4> HistogramHovered { private get; set; }

        internal override void SetCore(Context context)
        {
            if (Line.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.PlotLines, Line.Value.ToImGui());
            }
            if (LineHovered.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.PlotLinesHovered, LineHovered.Value.ToImGui());
            }
            if (Histogram.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Histogram.Value.ToImGui());
            }
            if (HistogramHovered.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.PlotHistogramHovered, HistogramHovered.Value.ToImGui());
            }
        }
    }
}

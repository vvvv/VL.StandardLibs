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

    /// <summary>
    /// The style applies to checkboxes, radio buttons, plots, sliders, text inputs
    /// </summary>
    [GenerateNode(Fragmented = false, Category = "ImGui.Styling", GenerateImmediate = false,
        Tags = "FrameBg FrameBgActive FrameBgHovered")]
    internal partial class SetFrameStyle : StyleBase
    {
        public Optional<Color4> Background { private get; set; }

        public Optional<Color4> Hovered { private get; set; }

        public Optional<Color4> Active { private get; set; }

        /// <summary>
        /// Padding within a framed rectangle (used by most widgets).
        /// </summary>
        public Optional<Vector2> Padding { private get; set; }

        /// <summary>
        /// Radius of frame corners rounding. Set to 0.0 to have rectangular frame (used by most widgets).
        /// </summary>
        public Optional<float> Rounding { private get; set; }

        /// <summary>
        /// Thickness of border around frames. Generally set to 0.0 or 0.01. (Other values are not well tested and more CPU/GPU costly).
        /// </summary>
        public Optional<float> BorderSize { private get; set; }

        internal override void SetCore(Context context)
        {
            if (Background.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.FrameBg, Background.Value.ToImGui());
            }
            if (Active.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Active.Value.ToImGui());
            }
            if (Hovered.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, Hovered.Value.ToImGui());
            }

            if (Padding.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Padding.Value.FromHectoToImGui());
            }
            if (Rounding.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, Rounding.Value.FromHectoToImGui());
            }
            if (BorderSize.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, BorderSize.Value.FromHectoToImGui());
            }
        }
    }
}

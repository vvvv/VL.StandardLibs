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
        Tags = "ScrollbarBg ScrollbarGrab ScrollbarGrabHovered ScrollbarGrabActive ScrollbarSize ScrollbarRounding")]
    internal partial class SetScrollStyle : StyleBase
    {
        public Optional<Color4> Backgorund { private get; set; }
               
        public Optional<Color4> GrabColor { private get; set; }

        public Optional<Color4> GrabHovered { private get; set; }

        public Optional<Color4> GrabActive { private get; set; }

        /// <summary>
        /// Width of the vertical scrollbar, Height of the horizontal scrollbar.
        /// </summary>
        public Optional<float> ScrollbarSize { private get; set; }

        /// <summary>
        /// Radius of grab corners for scrollbar.
        /// </summary>
        public Optional<float> GrabRounding { private get; set; }

        internal override void SetCore(Context context)
        {
            if (Backgorund.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, Backgorund.Value.ToImGui());
            }

            if (ScrollbarSize.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, ScrollbarSize.Value.FromHectoToImGui());
            }

            if (GrabColor.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, GrabColor.Value.ToImGui());
            }
            if (GrabHovered.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, GrabHovered.Value.ToImGui());
            }
            if (GrabActive.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, GrabActive.Value.ToImGui());
            }
            if (GrabRounding.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, GrabRounding.Value.FromHectoToImGui());
            }
        }
    }
}

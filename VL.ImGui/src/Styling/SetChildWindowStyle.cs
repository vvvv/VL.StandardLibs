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
        Tags = "ChildBg ChildRounding ChildBorderSize")]
    internal partial class SetChildWindowStyle : StyleBase
    {
        public Optional<Color4> Background { private get; set; }

        /// <summary>
        /// Radius of child window corners rounding. Set to 0.0 to have rectangular windows.
        /// </summary>
        public Optional<float> Rounding { private get; set; }

        /// <summary>
        /// Thickness of border around child windows. Generally set to 0.0 or 0.01. (Other values are not well tested and more CPU/GPU costly).
        /// </summary>
        public Optional<float> BorderSize { private get; set; }

        internal override void SetCore(Context context)
        {
            if (Background.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.ChildBg, Background.Value.ToImGui());
            }
            if (Rounding.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, Rounding.Value.FromHectoToImGui());
            }
            if (BorderSize.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, BorderSize.Value.FromHectoToImGui());
            }
        }
    }
}

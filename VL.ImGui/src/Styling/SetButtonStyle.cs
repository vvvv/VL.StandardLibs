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
        Tags = "ButtonHovered ButtonActive")]
    internal partial class SetButtonStyle : StyleBase
    {
        public Optional<Color4> Background { private get; set; }

        public Optional<Color4> Hovered { private get; set; }

        public Optional<Color4> Active { private get; set; }

        /// <summary>
        /// Alignment of button text when button is larger than text. Defaults to (0.5, 0.5) (centered).
        /// </summary>
        public Optional<Vector2> TextAlign { private get; set; }

        internal override void SetCore(Context context)
        {
            if (Background.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.Button, Background.Value.ToImGui());
            }
            if (Hovered.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Hovered.Value.ToImGui());
            }
            if (Active.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Active.Value.ToImGui());
            }

            if (TextAlign.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, TextAlign.Value.ToImGui());
            }
        }
    }
}

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
    /// Separator, generally horizontal. inside a menu bar or in horizontal layout mode, this becomes a vertical separator.
    /// </summary>
    [GenerateNode(Fragmented = false, Category = "ImGui.Styling", GenerateImmediate = false)]
    internal partial class SetSeparatorStyle : StyleBase
    {
        public Optional<Color4> Color { private get; set; }

        public Optional<Color4> Hovered { private get; set; }

        public Optional<Color4> Active { private get; set; }

        internal override void SetCore(Context context)
        {
            if (Color.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.Separator, Color.Value.ToImGui());
            }
            if (Hovered.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.SeparatorHovered, Hovered.Value.ToImGui());
            }
            if (Active.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.SeparatorActive, Active.Value.ToImGui());
            }
        }
    }
}

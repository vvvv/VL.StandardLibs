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
    /// This style applies to popups, menus, tooltips windows.
    /// </summary>
    [GenerateNode(Fragmented = false, Category = "ImGui.Styling", GenerateImmediate = false,
        Tags = "Menu Tooltip PopupBg PopupRounding PopupBorderSize")]
    internal partial class SetPopupStyle : StyleBase
    {
        /// <summary>
        /// Background of popups, menus, tooltips windows
        /// </summary>
        public Optional<Color4> Background { private get; set; }

        /// <summary>
        /// Darken/colorize entire screen behind a modal window, when one is active
        /// </summary>
        public Optional<Color4> ModalDimBackground { private get; set; }

        /// <summary>
        /// Radius of popup window corners rounding.
        /// </summary>
        public Optional<float> Rounding { private get; set; }

        /// <summary>
        /// Thickness of border around popup/tooltip windows. Generally set to 0.0 or 0.01. (Other values are not well tested and more CPU/GPU costly).
        /// </summary>
        public Optional<float> BorderSize { private get; set; }

        internal override void SetCore(Context context)
        {
            if (Background.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.PopupBg, Background.Value.ToImGui());
            }
            if (ModalDimBackground.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.ModalWindowDimBg, ModalDimBackground.Value.ToImGui());
            }
            if (Rounding.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, Rounding.Value.FromHectoToImGui());
            }
            if (BorderSize.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, BorderSize.Value.FromHectoToImGui());
            }
        }
    }
}

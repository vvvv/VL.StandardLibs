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
        Tags = "WindowBg MenuBarBg WindowMinSize WindowTitleAlign WindowPadding WindowRounding WindowBorderSize")]
    internal partial class SetWindowStyle : StyleBase
    {
        public Optional<Color4> Background { private get; set; }

        public Optional<Color4> MenuBarBackground { private get; set; }

        public Optional<Color4> TitleBackground { private get; set; }

        public Optional<Color4> TitleBackgroundActive { private get; set; }

        public Optional<Color4> TitleBackgroundCollapsed { private get; set; }

        /// <summary>
        /// Color of the Resize grip in lower-right and lower-left corners of windows.
        /// </summary>
        public Optional<Color4> ResizeGripColor { private get; set; }

        /// <summary>
        /// Hovered Color of the Resize grip.
        /// </summary>
        public Optional<Color4> ResizeGripHovered { private get; set; }

        /// <summary>
        /// Active Color of the Resize grip.
        /// </summary>
        public Optional<Color4> ResizeGripActive { private get; set; }

        /// <summary>
        /// Minimum window size.
        /// </summary>
        public Optional<Vector2> MinSize { private get; set; }

        public Optional<Vector2> TitleAlign { private get; set; }

        /// <summary>
        /// Padding within a window.
        /// </summary>
        public Optional<Vector2> Padding { private get; set; }

        /// <summary>
        /// Radius of window corners rounding. Set to 0.0 to have rectangular windows. Large values tend to lead to variety of artifacts and are not recommended.
        /// </summary>
        public Optional<float> Rounding { private get; set; }

        /// <summary>
        /// Thickness of border around windows. Generally set to 0.0 or 0.01. (Other values are not well tested and more CPU/GPU costly).
        /// </summary>
        public Optional<float> BorderSize { private get; set; }

        internal override void SetCore(Context context)
        {
            if (Background.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.WindowBg, Background.Value.ToImGui());
            }
            if (MenuBarBackground.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.MenuBarBg, MenuBarBackground.Value.ToImGui());
            }
            if (ResizeGripColor.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.ResizeGrip, ResizeGripColor.Value.ToImGui());
            }
            if (ResizeGripHovered.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered, ResizeGripHovered.Value.ToImGui());
            }
            if (ResizeGripActive.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.ResizeGripActive, ResizeGripActive.Value.ToImGui());
            }
            if (TitleBackground.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.TitleBg, TitleBackground.Value.ToImGui());
            }
            if (TitleBackgroundActive.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.TitleBgActive, TitleBackgroundActive.Value.ToImGui()); ;
            }
            if (TitleBackgroundCollapsed.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, TitleBackgroundCollapsed.Value.ToImGui());
            }
            if (MinSize.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, MinSize.Value.FromHectoToImGui());
            }
            if (TitleAlign.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.WindowTitleAlign, TitleAlign.Value.ToImGui());
            }
            if (Padding.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Padding.Value.FromHectoToImGui());
            }
            if (Rounding.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, Rounding.Value.FromHectoToImGui());
            }
            if (BorderSize.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, BorderSize.Value.FromHectoToImGui());
            }
        }
    }
}

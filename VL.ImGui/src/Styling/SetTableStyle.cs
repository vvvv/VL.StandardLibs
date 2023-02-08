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
        Tags = "TableHeaderBg TableRowBg TableRowBgAlt TableBorderStrong TableBorderLight CellPadding")]
    internal partial class SetTableStyle : StyleBase
    {
        public Optional<Color4> HeaderBackground { private get; set; }

        /// <summary>
        /// Table row background (even rows)
        /// </summary>
        public Optional<Color4> RowBackground { private get; set; }

        /// <summary>
        /// Table row background (odd rows)
        /// </summary>
        public Optional<Color4> RowBackgroundAlt { private get; set; }

        /// <summary>
        /// Table outer and header borders (prefer using Alpha=1.0 here)
        /// </summary>
        public Optional<Color4> BorderStrongColor { private get; set; }

        /// <summary>
        /// Table inner borders (prefer using Alpha=1.0 here)
        /// </summary>
        public Optional<Color4> BorderLightColor { private get; set; }

        /// <summary>
        /// Padding within a table cell.
        /// </summary>
        public Optional<Vector2> CellPadding { private get; set; }

        internal override void SetCore(Context context)
        {
            if (HeaderBackground.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, HeaderBackground.Value.ToImGui());
            }
            if (RowBackground.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.TableRowBg, RowBackground.Value.ToImGui());
            }
            if (RowBackgroundAlt.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, RowBackgroundAlt.Value.ToImGui());
            }
            if (BorderStrongColor.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, BorderStrongColor.Value.ToImGui());
            }
            if (BorderLightColor.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.TableBorderLight, BorderLightColor.Value.ToImGui());
            }

            if (CellPadding.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, CellPadding.Value.FromHectoToImGui());
            }
        }
    }
}

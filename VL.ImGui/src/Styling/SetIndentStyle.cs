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
        Tags = "IndentSpacing")]
    internal partial class SetIndentStyle : StyleBase
    {
        /// <summary>
        /// Horizontal indentation when e.g. entering a tree node. Generally == (FontSize + FramePadding.x*2).
        /// </summary>
        public Optional<float> Indent { private get; set; }

        internal override void SetCore(Context context)
        {
            if (Indent.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, Indent.Value);
            }
        }
    }
}

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
        Tags = "SelectableTextAlign")]
    internal partial class SetSelectableStyle : StyleBase
    {
        /// <summary>
        /// Alignment of selectable text. Defaults to (0.0, 0.0) (top-left aligned). It's generally important to keep this left-aligned if you want to lay multiple items on a same line.
        /// </summary>
        public Optional<Vector2> TextAlign { private get; set; }

        internal override void SetCore(Context context)
        {
            if (TextAlign.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, TextAlign.Value.ToImGui());
            }
        }
    }
}

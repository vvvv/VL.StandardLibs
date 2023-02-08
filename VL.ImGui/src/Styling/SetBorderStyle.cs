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
        Tags = "BorderShadow")]
    internal partial class SetBorderStyle : StyleBase
    {
        public Optional<Color4> Color { private get; set; }

        public Optional<Color4> Shadow { private get; set; }

        internal override void SetCore(Context context)
        {
            if (Color.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.Border, Color.Value.ToImGui());
            }
            if (Shadow.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.BorderShadow, Shadow.Value.ToImGui());
            }
        }
    }
}

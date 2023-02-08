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
        Tags = "DisabledAlpha")]
    internal partial class SetAlphaStyle : StyleBase
    {
        /// <summary>
        /// Global alpha applies to everything in Dear ImGui.
        /// </summary>
        public Optional<float> Alpha { private get; set; }

        /// <summary>
        /// Additional alpha multiplier. Multiply over current value of Alpha.
        /// </summary>
        public Optional<float> DisabledAlpha { private get; set; }

        internal override void SetCore(Context context)
        {
            if (Alpha.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, Alpha.Value);
            }
            if (DisabledAlpha.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.DisabledAlpha, DisabledAlpha.Value);
            }
        }
    }
}

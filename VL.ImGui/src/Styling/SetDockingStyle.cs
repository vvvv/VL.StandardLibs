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
    [GenerateNode(Fragmented = false, Category = "ImGui.Styling", GenerateImmediate = false)]
    internal partial class SetDockingStyle : StyleBase
    {
        public Optional<float> SeparatorSize { private get; set; }

        internal override void SetCore(Context context)
        {
            if (SeparatorSize.HasValue)
            {
                valueCount++;
                ImGui.PushStyleVar(ImGuiStyleVar.DockingSeparatorSize, SeparatorSize.Value.FromHectoToImGui());
            }
        }
    }
}

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
        Tags = "SliderGrab SliderGrabActive")]
    internal partial class SetSliderStyle : StyleBase
    {
        public Optional<Color4> Color { private get; set; }

        public Optional<Color4> Active { private get; set; }

        internal override void SetCore(Context context)
        {
            if (Color.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.SliderGrab, Color.Value.ToImGui());
            }
            if (Active.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, Active.Value.ToImGui());
            }
        }
    }
}

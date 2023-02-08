using Stride.Core.Mathematics;
using VL.Core;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    /// <summary>
    /// Change the color of a cell, row, or column. See ImGuiTableBgTarget_ flags for details.
    /// </summary>
    internal partial class TableSetBgColor : Widget
    {

        public ImGuiNET.ImGuiTableBgTarget Target { private get; set; }

        public Optional<Color4> Color { private get; set; }

        public int Column { private get; set; } = -1;

        internal override void UpdateCore(Context context)
        {
            var uintColor = ImGuiNET.ImGui.GetColorU32(Color.Value.ToImGui());

            ImGuiNET.ImGui.TableSetBgColor(Target, uintColor, Column);
        }
    }
}
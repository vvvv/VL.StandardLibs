using ImGuiNET;
using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (Vector4)", Category = "ImGui.Widgets", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragVector4 : DragWidget<Vector4, float>
    {
        protected override bool Drag(string label, ref Vector4 value, float speed, float min, float max, string? format, ImGuiSliderFlags flags)
        {
            var x = value.ToImGui();
            if (ImGuiNET.ImGui.DragFloat4(label, ref x, speed, min, max, format, flags))
            {
                value = x.ToVL();
                return true;
            }
            return false;
        }
    }
}

using ImGuiNET;
using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (Int3)", Category = "ImGui.Widgets", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragInt3 : DragWidget<Int3, int>
    {
        protected override bool Drag(string label, ref Int3 value, float speed, int min, int max, string? format, ImGuiSliderFlags flags)
        {
            ref var x = ref value.X;
            if (ImGuiNET.ImGui.DragInt3(label, ref x, speed, min, max, format, flags))
            {
                value = Unsafe.As<int, Int3>(ref x);
                return true;
            }
            return false;
        }
    }
}

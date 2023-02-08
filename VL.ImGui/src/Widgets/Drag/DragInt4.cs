using ImGuiNET;
using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (Int4)", Category = "ImGui.Widgets", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragInt4 : DragWidget<Int4, int>
    {
        protected override bool Drag(string label, ref Int4 value, float speed, int min, int max, string? format, ImGuiSliderFlags flags)
        {
            ref var x = ref value.X;
            if (ImGuiNET.ImGui.DragInt2(label, ref x, speed, min, max, format, flags))
            {
                value = Unsafe.As<int, Int4>(ref x);
                return true;
            }
            return false;
        }
    }
}

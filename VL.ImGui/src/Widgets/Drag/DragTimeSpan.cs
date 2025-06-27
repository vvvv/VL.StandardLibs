using ImGuiNET;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (TimeSpan)", Category = "ImGui.Widgets.Advanced")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragTimeSpan : DragWidget_Weak<TimeSpan, TimeSpan>
    {
        public DragTimeSpan() 
        {
            Format = "%.3f s";
        }

        protected override bool Drag(string label, ref TimeSpan value, float speed, TimeSpan min, TimeSpan max, string? format, ImGuiSliderFlags flags)
        {
            var seconds = value.TotalSeconds;
            if (ImGuiUtils.DragDouble(label, ref seconds, speed, min.TotalSeconds, max.TotalSeconds, format, flags))
            {
                value = TimeSpan.FromSeconds(seconds);
                return true;
            }
            return false;
        }
    }
}

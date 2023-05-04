using ImGuiNET;
using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    internal abstract class VerticalSliderWidget<T, TComponent> : ChannelWidget<T>, IHasLabel
    {
        public VerticalSliderWidget(TComponent min, TComponent max, Vector2 size)
        {
            Min = min;
            Max = max;
            Size = size;
        }

        public string? Label { get; set; }

        public TComponent Min { protected get; set; }

        public TComponent Max { protected get; set; }

        public Vector2 Size { get; set; }

        /// <summary>
        /// Adjust format string to decorate the value with a prefix, a suffix, or adapt the editing and display precision e.g. "%.3f" -> 1.234; "%5.2f secs" -> 01.23 secs; "Biscuit: % .0f" -> Biscuit: 1; etc.
        /// </summary>
        public string? Format { protected get; set; }

        public ImGuiSliderFlags Flags { protected get; set; }
    }
}

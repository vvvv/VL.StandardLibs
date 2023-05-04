using ImGuiNET;

namespace VL.ImGui.Widgets
{
    internal abstract class SliderWidget<T, TComponent> : ChannelWidget<T>, IHasLabel
    {
        public SliderWidget(TComponent min, TComponent max)
        {
            Min = min; 
            Max = max;
        }

        public string? Label { get; set; }

        public TComponent Min { protected get; set; }

        public TComponent Max { protected get; set; }

        /// <summary>
        /// Adjust format string to decorate the value with a prefix, a suffix, or adapt the editing and display precision e.g. "%.3f" -> 1.234; "%5.2f secs" -> 01.23 secs; "Biscuit: % .0f" -> Biscuit: 1; etc.
        /// </summary>
        public string? Format { protected get; set; }

        public ImGuiSliderFlags Flags { protected get; set; }
    }
}

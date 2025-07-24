using ImGuiNET;
using VL.Core;

namespace VL.ImGui.Widgets
{
    internal abstract class SliderWidget<T, TComponent> : ChannelWidget<T> 
        where TComponent : unmanaged, System.Numerics.IMinMaxValue<TComponent>
    {
        protected readonly MinValueSelector<TComponent> min;
        protected readonly MaxValueSelector<TComponent> max;

        public SliderWidget(TComponent min, TComponent max)
        {
            AddValueSelector(this.min = new(min));
            AddValueSelector(this.max = new(max)); 
        }

        public Optional<TComponent> Min { protected get => default; set => min.SetPinValue(value); }

        public Optional<TComponent> Max { protected get => default; set => max.SetPinValue(value); }

        /// <summary>
        /// Adjust format string to decorate the value with a prefix, a suffix, or adapt the editing and display precision e.g. "%.3f" -> 1.234; "%5.2f secs" -> 01.23 secs; "Biscuit: % .0f" -> Biscuit: 1; etc.
        /// </summary>
        public string? Format { protected get; set; }

        public ImGuiSliderFlags Flags { protected get; set; }
    }
}

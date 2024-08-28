using ImGuiNET;
using Stride.Core.Mathematics;
using VL.Core;

namespace VL.ImGui.Widgets
{
    internal abstract class VerticalSliderWidget<T, TComponent> : ChannelWidget<T>
    {
        protected readonly MinValueSelector<TComponent> min;
        protected readonly MaxValueSelector<TComponent> max;

        public VerticalSliderWidget(TComponent min, TComponent max, Vector2 size)
        {
            AddValueSelector(this.min = new(min));
            AddValueSelector(this.max = new(max));
            Size = size;
        }

        public Optional<TComponent> Min { protected get => default; set => min.SetPinValue(value); }

        public Optional<TComponent> Max { protected get => default; set => max.SetPinValue(value); }

        public Vector2 Size { get; set; }

        /// <summary>
        /// Adjust format string to decorate the value with a prefix, a suffix, or adapt the editing and display precision e.g. "%.3f" -> 1.234; "%5.2f secs" -> 01.23 secs; "Biscuit: % .0f" -> Biscuit: 1; etc.
        /// </summary>
        public string? Format { protected get; set; }

        public ImGuiSliderFlags Flags { protected get; set; }
    }
}

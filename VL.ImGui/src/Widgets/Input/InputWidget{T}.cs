using ImGuiNET;
using VL.Core;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    internal abstract class InputWidget<T> : ChannelWidget<T>
         where T : unmanaged
    {
        public Optional<T> Min { protected get => default; set => min.SetPinValue(value); }        
        public Optional<T> Max { protected get => default; set => max.SetPinValue(value); }

        protected readonly MinValueSelector<T> min;
        protected readonly MaxValueSelector<T> max;

        public InputWidget(T min, T max)
        {
            AddValueSelector(this.min = new(default));
            AddValueSelector(this.max = new(default));
        }
    }
}
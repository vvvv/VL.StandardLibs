using ImGuiNET;
using VL.Core;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    internal abstract class InputWidget<T> : ChannelWidget<T>
         where T : unmanaged, System.Numerics.IMinMaxValue<T>
    {
        protected readonly MinValueSelector<T> min;
        protected readonly MaxValueSelector<T> max;

        public InputWidget()
        {
            AddValueSelector(this.min = new(default));
            AddValueSelector(this.max = new(default));
        }
    }

    internal abstract class InputWidget_Simple<T> : ChannelWidget<T>
        where T : unmanaged
    {
        protected readonly MinValueSelector_Simple<T> min;
        protected readonly MaxValueSelector_Simple<T> max;

        public InputWidget_Simple(T min, T max)
        {
            AddValueSelector(this.min = new(min));
            AddValueSelector(this.max = new(max));
        }

        public Optional<T> Min { protected get => default; set => min.SetPinValue(value); }

        public Optional<T> Max { protected get => default; set => max.SetPinValue(value); }
    }
}
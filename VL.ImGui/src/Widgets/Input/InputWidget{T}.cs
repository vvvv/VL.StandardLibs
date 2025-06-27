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
            this.min.Max = this.max;
            this.max.Min = this.min;
        }
    }

    internal abstract class InputWidget_Weak<T> : ChannelWidget<T>
        where T : unmanaged
    {
        protected readonly MinValueSelector_Weak<T> min;
        protected readonly MaxValueSelector_Weak<T> max;

        public InputWidget_Weak()
        {
            AddValueSelector(this.min = new(default));
            AddValueSelector(this.max = new(default));
        }

        public Optional<T> Min { protected get => default; set => min.SetPinValue(value); }

        public Optional<T> Max { protected get => default; set => max.SetPinValue(value); }
    }
}
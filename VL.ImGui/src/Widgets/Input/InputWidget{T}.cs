using ImGuiNET;
using VL.Core;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    internal abstract class InputWidget<T> : ChannelWidget<T>
         where T : unmanaged
    {
        protected readonly MinValueSelector<T> min;
        protected readonly MaxValueSelector<T> max;

        public InputWidget()
        {
            AddValueSelector(this.min = new(default));
            AddValueSelector(this.max = new(default));
        }

        public Optional<T> Min { protected get => default; set => min.SetPinValue(value); }

        public Optional<T> Max { protected get => default; set => max.SetPinValue(value); }
    }
}

using VL.Core;

namespace VL.ImGui.Widgets
{
    abstract class ValueSelector<T> : ValueSelector
    {
        private readonly T fallback;
        private Optional<T> attributeValue;
        private Optional<T> pinValue;

        public ValueSelector(T fallback)
        {
            this.fallback = fallback;
        }

        public T Value => pinValue.HasValue ? pinValue.Value : attributeValue.HasValue ? attributeValue.Value : fallback;

        public void SetPinValue(Optional<T> value) => pinValue = value;

        public void SetAttributeValue(Optional<T> value) => attributeValue = value;

        public bool HasValue => pinValue.HasValue | attributeValue.HasValue;
    }
}

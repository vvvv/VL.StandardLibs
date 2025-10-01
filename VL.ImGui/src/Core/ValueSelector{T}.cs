using VL.Core;

namespace VL.ImGui.Widgets
{
    abstract class ValueSelector<T> : ValueSelector
    {
        protected readonly T fallback;
        private Optional<T> attributeValue;
        private Optional<T> pinValue;

        public ValueSelector(T fallback)
        {
            this.fallback = fallback;
        }

        public virtual T Value => pinValue.HasValue ? pinValue.Value : attributeValue.HasValue ? attributeValue.Value : fallback;
        public Optional<T> OptionalValue => pinValue.HasValue ? pinValue : attributeValue.HasValue ? attributeValue : default;

        public void SetPinValue(Optional<T> value) => pinValue = value;

        public void SetAttributeValue(Optional<T> value) => attributeValue = value;

        public bool HasValue => pinValue.HasValue | attributeValue.HasValue;
    }
}

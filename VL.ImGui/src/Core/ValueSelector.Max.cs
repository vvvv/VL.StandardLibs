using System.Numerics;
using VL.Core.EditorAttributes;
using VL.Lib.Collections;

namespace VL.ImGui.Widgets
{
    sealed class MaxValueSelector<T> : ValueSelector<T>
    {
        public MaxValueSelector(T fallback) : base(fallback)
        {
        }

        public override void Update(Spread<Attribute> attributes)
        {
            SetAttributeValue(default);

            foreach (var a in attributes)
                if (a is MaxAttribute max)
                    SetAttributeValue(max.GetValue<T>());
        }

        public override T Value => HasValue ? base.Value : fallback;
    }
}

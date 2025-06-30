using System.Numerics;
using VL.Core.EditorAttributes;
using VL.Lib.Collections;

namespace VL.ImGui.Widgets
{
    sealed class MaxValueSelector<T> : ValueSelector<T>
        where T : IMinMaxValue<T>
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

        public override T Value => HasValue ? base.Value : T.MaxValue;
    }

    sealed class MaxValueSelector_Simple<T> : ValueSelector<T>
    {
        public MaxValueSelector_Simple(T fallback) : base(fallback)
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

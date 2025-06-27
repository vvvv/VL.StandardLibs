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
                if (a is MaxAttribute min)
                    SetAttributeValue(min.GetValue<T>());
        }

        public MinValueSelector<T> Min { get; set; }

        public override T Value => Min.HasValue ? T.MaxValue : base.Value;
    }

    sealed class MaxValueSelector_Weak<T> : ValueSelector<T>
    {
        public MaxValueSelector_Weak(T fallback) : base(fallback)
        {
        }

        public override void Update(Spread<Attribute> attributes)
        {
            SetAttributeValue(default);

            foreach (var a in attributes)
                if (a is MaxAttribute min)
                    SetAttributeValue(min.GetValue<T>());
        }
    }
}

using System.Numerics;
using VL.Core.EditorAttributes;
using VL.Lib.Collections;

namespace VL.ImGui.Widgets
{
    sealed class MinValueSelector<T> : ValueSelector<T>
        where T : IMinMaxValue<T>
    {
        public MinValueSelector(T fallback) : base(fallback)
        {
        }

        public override void Update(Spread<Attribute> attributes)
        {
            SetAttributeValue(default);

            foreach (var a in attributes)
                if (a is MinAttribute min)
                    SetAttributeValue(min.GetValue<T>());
        }

        public MaxValueSelector<T> Max { get; set; }

        public override T Value => Max.HasValue ? T.MinValue : base.Value;
    }

    sealed class MinValueSelector_Weak<T> : ValueSelector<T>
    {
        public MinValueSelector_Weak(T fallback) : base(fallback)
        {
        }

        public override void Update(Spread<Attribute> attributes)
        {
            SetAttributeValue(default);

            foreach (var a in attributes)
                if (a is MinAttribute min)
                    SetAttributeValue(min.GetValue<T>());
        }
    }
}

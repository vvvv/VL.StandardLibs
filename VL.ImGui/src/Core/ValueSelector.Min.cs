using VL.Core.EditorAttributes;
using VL.Lib.Collections;

namespace VL.ImGui.Widgets
{
    sealed class MinValueSelector<T> : ValueSelector<T>
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
    }
}

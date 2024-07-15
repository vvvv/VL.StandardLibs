using VL.Core.EditorAttributes;
using VL.Lib.Collections;

namespace VL.ImGui.Widgets
{
    sealed class LabelSelector : ValueSelector<string?>
    {
        public LabelSelector() : base(default)
        {
        }

        public override void Update(Spread<Attribute> attributes)
        {
            SetAttributeValue(default);

            foreach (var a in attributes)
                if (a is LabelAttribute min)
                    SetAttributeValue(min.Label);
        }
    }
}

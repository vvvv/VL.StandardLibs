using System;

namespace VL.Core.EditorAttributes
{
    public sealed class LabelAttribute : Attribute
    {
        public LabelAttribute(string label)
        {
            Label = label;
        }

        public string Label { get; }
    }
}

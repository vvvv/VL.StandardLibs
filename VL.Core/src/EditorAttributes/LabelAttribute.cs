using System;

namespace VL.Core.EditorAttributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class LabelAttribute : Attribute
    {
        public LabelAttribute(string label)
        {
            Label = label;
        }

        public string Label { get; }
    }
}

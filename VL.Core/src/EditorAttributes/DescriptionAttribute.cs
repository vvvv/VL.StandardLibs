using System;

namespace VL.Core.EditorAttributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; }

        public override string ToString()
        {
            return $"Description: {Description}";
        }
    }
}

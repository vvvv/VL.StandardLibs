using System;

namespace VL.Core.Import
{
    /// <summary>
    /// Allows to specify a custom name for a symbol.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class NameAttribute : Attribute
    {
        public NameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}

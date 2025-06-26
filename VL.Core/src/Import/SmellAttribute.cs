using System;

namespace VL.Core.Import
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class SmellAttribute : Attribute
    {
        public SmellAttribute(SymbolSmell smell)
        {
            Smell = smell;
        }

        public SymbolSmell Smell { get; }
    }
}

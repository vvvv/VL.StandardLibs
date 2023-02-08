using System;

namespace VL.Core.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class AssemblyInitializerAttribute : Attribute
    {
        public Type Initializer { get; }

        public AssemblyInitializerAttribute(Type initializer)
        {
            Initializer = initializer;
        }
    }
}

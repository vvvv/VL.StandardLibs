using System;

namespace VL.Core.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CreateNewAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CreateDefaultAttribute : Attribute
    {
    }
}


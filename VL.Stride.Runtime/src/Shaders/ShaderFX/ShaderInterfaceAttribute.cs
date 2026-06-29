using System;
using System.Collections.Generic;
using System.Text;

namespace VL.Stride.Shaders.ShaderFX;

[AttributeUsage(AttributeTargets.Interface)]
public class ShaderInterfaceAttribute : Attribute
{
    public string NameOfInterfaceInSDSL { get; }

    public ShaderInterfaceAttribute(string nameOfInterfaceInSDSL)
    {
        NameOfInterfaceInSDSL = nameOfInterfaceInSDSL;
    }
}

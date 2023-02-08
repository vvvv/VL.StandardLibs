using System.Collections.Generic;
using Stride.Core;
using Stride.Rendering;
using Stride.Rendering.Materials;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;


namespace VL.Stride.Shaders.ShaderFX
{
    public static class GenericValueKeys<T> where T : struct
    {
        static GenericValueKeys()
        {
            GenericValueParameter = ParameterKeys.NewValue<T>(default, "ShaderFX.InputValue" + GetNameForType<T>());
            ParameterKeys.Merge(GenericValueParameter, typeof(GenericValueKeys<T>), GenericValueParameter.Name);
        }

        [DataMemberIgnore]
        public static readonly ValueParameterKey<T> GenericValueParameter;
    }
}

using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;


namespace VL.Stride.Shaders.ShaderFX
{
    public static class GenericValueKeys<T> where T : struct
    {
        static GenericValueKeys()
        {
            var name = "ShaderFX.InputValue" + GetNameForType<T>();
            if (typeof(T) == typeof(Color4))
                name += "_Color";

            GenericValueParameter = ParameterKeys.NewValue<T>(default, name);
            ParameterKeys.Merge(GenericValueParameter, typeof(GenericValueKeys<T>), GenericValueParameter.Name);
        }

        [DataMemberIgnore]
        public static readonly ValueParameterKey<T> GenericValueParameter;
    }
}

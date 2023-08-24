using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;

namespace VL.Stride.Shaders.ShaderFX
{
    public class InputValue<T> : ComputeValue<T>
        where T : struct
    {
        private readonly ValueParameterUpdater<T> updater = new ValueParameterUpdater<T>(default(ShaderGeneratorContext));

        public InputValue(ValueParameterKey<T> key = null, string constantBufferName = null)
        {
            Key = key;
            ConstantBufferName = constantBufferName;
        }

        /// <summary>
        /// Can be updated from mainloop
        /// </summary>
        public T Input
        { 
            get => updater.Value;
            set => updater.Value = value;
        }

        public ValueParameterKey<T> Key { get; }
        public string ConstantBufferName { get; private set; }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            ShaderClassSource shaderClassSource;

            if (Key == null)
            {
                var usedKey = GetInputKey(context);

                // keep track of the parameters
                updater.Track(context, usedKey);

                // find constant buffer name
                var constantBufferName = ConstantBufferName;

                if (string.IsNullOrWhiteSpace(constantBufferName))
                {
                    constantBufferName = context is MaterialGeneratorContext ? "PerMaterial" : "PerUpdate";
                }

                shaderClassSource = GetShaderSourceForType<T>("Input", usedKey, constantBufferName);
            }
            else
            {
                shaderClassSource = GetShaderSourceForType<T>("InputKey", Key);
            }


            return shaderClassSource;
        }

        private ValueParameterKey<T> GetInputKey(ShaderGeneratorContext context)
        {
            return (ValueParameterKey<T>)context.GetParameterKey(Key ?? GenericValueKeys<T>.GenericValueParameter);
        }
    }
}

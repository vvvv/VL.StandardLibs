using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;
using System;

namespace VL.Stride.Shaders.ShaderFX
{
    public class DeclResource<T> : ComputeNode<T>, IComputeVoid
        where T : class
    {
        readonly ObjectParameterUpdater<T> updater = new ObjectParameterUpdater<T>(default(ShaderGeneratorContext));
        readonly string resourceGroupName;

        public DeclResource(string resourceGroupName = null)
        {
            this.resourceGroupName = resourceGroupName;
        }

        /// <summary>
        /// Can be updated from mainloop
        /// </summary>
        public T Resource
        {
            get => updater.Value;
            set => updater.Value = value;
        }

        public ObjectParameterKey<T> Key { get; private set; }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            Key = context.GetKeyForContext(this, Key);

            //track the parameter collection
            updater.Track(context, Key);

            //no shader source to create here, only the key
            return new ShaderClassSource("ComputeVoid");
        }

        public virtual string GetResourceGroupName(ShaderGeneratorContext context)
        {
            if (string.IsNullOrWhiteSpace(resourceGroupName))
            {
                return context is MaterialGeneratorContext ? "PerMaterial" : "PerUpdate";
            }

            return resourceGroupName;
        }

        public override string ToString()
        {
            return $"{typeof(T).Name} {Key?.Name}";
        }
    }
}

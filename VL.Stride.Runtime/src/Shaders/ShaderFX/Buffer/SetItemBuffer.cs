using System;
using System.Collections.Generic;
using System.Text;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;
using Buffer = Stride.Graphics.Buffer;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;

namespace VL.Stride.Shaders.ShaderFX
{
    public class SetItemRWBuffer<T> : ComputeVoid
    {
        public SetItemRWBuffer(DeclBuffer buffer, IComputeValue<T> value, IComputeValue<uint> index)
        {
            BufferDecl = buffer;
            Value = value;
            Index = index;
        }

        public DeclBuffer BufferDecl { get; }

        public IComputeValue<T> Value { get; }
        public IComputeValue<uint> Index { get; }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {

            if (BufferDecl == null)
                return GetShaderSourceForType<T>("Compute");

            BufferDecl.GenerateShaderSource(context, baseKeys);

            var shaderClassSource = GetShaderSourceForType<T>("SetItemRWBuffer", BufferDecl.Key, BufferDecl.GetResourceGroupName(context));

            var mixin = shaderClassSource.CreateMixin();

            mixin.AddComposition(Value, "Value", context, baseKeys);
            mixin.AddComposition(Index, "Index", context, baseKeys);

            return mixin; 
        }

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return ReturnIfNotNull(BufferDecl, Value, Index);
        }
    }
}

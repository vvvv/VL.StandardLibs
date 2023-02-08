using System;
using System.Collections.Generic;
using System.Text;
using Stride.Rendering.Materials;
using Stride.Shaders;
using Stride.Core.Mathematics;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;

namespace VL.Stride.Shaders.ShaderFX
{
    public class JoinVector4 : Join<Vector4>
    {
        public JoinVector4(IComputeValue<float> x, IComputeValue<float> y, IComputeValue<float> z, IComputeValue<float> w) 
            : base(x, y, z, w)
        {
        }
    }

    public class Join<T> : ComputeValue<T>
    {
        public Join(IComputeValue<float> x, IComputeValue<float> y, IComputeValue<float> z, IComputeValue<float> w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;

            ShaderName = "Join";
        }

        public IComputeValue<float> X { get; }
        public IComputeValue<float> Y { get; }
        public IComputeValue<float> Z { get; }
        public IComputeValue<float> W { get; }

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return ReturnIfNotNull(X, Y, Z, W);
        }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderSource = GetShaderSourceForType<T>(ShaderName);

            var mixin = shaderSource.CreateMixin();

            mixin.AddComposition(X, "x", context, baseKeys);
            mixin.AddComposition(Y, "y", context, baseKeys);
            mixin.AddComposition(Z, "z", context, baseKeys);
            mixin.AddComposition(W, "w", context, baseKeys);

            return mixin;
        }
    }

    public class JoinVector4Vector3 : ComputeValue<Vector4>
    {
        public JoinVector4Vector3(IComputeValue<Vector3> v, IComputeValue<float> w)
        {
            V = v;
            W = w;

            ShaderName = "JoinFloat4Float3";
        }

        public IComputeValue<Vector3> V { get; }
        public IComputeValue<float> W { get; }

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return ReturnIfNotNull(V, W);
        }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderSource = new ShaderClassSource(ShaderName);

            var mixin = shaderSource.CreateMixin();

            mixin.AddComposition(V, "v", context, baseKeys);
            mixin.AddComposition(W, "w", context, baseKeys);

            return mixin;
        }
    }
}

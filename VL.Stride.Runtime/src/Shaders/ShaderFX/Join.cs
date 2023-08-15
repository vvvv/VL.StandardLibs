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
        public JoinVector4(IComputeNode<float> x, IComputeNode<float> y, IComputeNode<float> z, IComputeNode<float> w) 
            : base(x, y, z, w)
        {
        }
    }

    public class Join<T> : ComputeValue<T>
    {
        public Join(IComputeNode<float> x, IComputeNode<float> y, IComputeNode<float> z, IComputeNode<float> w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;

            ShaderName = "Join";
        }

        public IComputeNode<float> X { get; }
        public IComputeNode<float> Y { get; }
        public IComputeNode<float> Z { get; }
        public IComputeNode<float> W { get; }

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
        public JoinVector4Vector3(IComputeNode<Vector3> v, IComputeNode<float> w)
        {
            V = v;
            W = w;

            ShaderName = "JoinFloat4Float3";
        }

        public IComputeNode<Vector3> V { get; }
        public IComputeNode<float> W { get; }

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

using System;
using System.Collections.Generic;
using System.Text;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;
using Buffer = Stride.Graphics.Buffer;
using Stride.Core.Mathematics;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;


namespace VL.Stride.Shaders.ShaderFX
{
    public enum SampleMode
    {
        Sampler,
        CubicBSpline,
        CubicCatmullRom
    }

    public class SampleTexture<T> : ComputeValue<T>
    {

        public SampleTexture(DeclTexture texture, DeclSampler sampler, IComputeValue<Vector2> texCoord, IComputeValue<float> lod, bool isRW = false, bool isSampleLevel = false, SampleMode sampleMode = SampleMode.Sampler)
        {
            TextureDecl = texture;
            SamplerDecl = sampler;
            TexCd = texCoord;
            LOD = lod;
            IsRW = isRW;
            IsSampleLevel = isSampleLevel;
            SampleMode = sampleMode;

            ShaderName = isSampleLevel ? "SampleLevelTexture" : "SampleTexture";
            ShaderName = IsRW ? ShaderName + "RW" : ShaderName;
        }

        public DeclTexture TextureDecl { get; }

        public DeclSampler SamplerDecl { get; }

        public IComputeValue<Vector2> TexCd { get; }

        public IComputeValue<float> LOD { get; }

        public bool IsRW { get; }

        public bool IsSampleLevel { get; }

        public SampleMode SampleMode { get; }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            if (TextureDecl == null || SamplerDecl == null)
                return GetShaderSourceForType<T>("Compute");

            TextureDecl.GenerateShaderSource(context, baseKeys);
            SamplerDecl.GenerateShaderSource(context, baseKeys);

            var isSampleLevel = IsSampleLevel || context.IsNotPixelStage;

            var shaderName = isSampleLevel ? "SampleLevelTexture" : "SampleTexture";

            switch (SampleMode) // cubic samplers also work in vertex shaders because they use SampleLevel 0
            {
                case SampleMode.Sampler:
                    break;
                case SampleMode.CubicBSpline:
                    shaderName = "SampleCubicBSplineTexture";
                    break;
                case SampleMode.CubicCatmullRom:
                    shaderName = "SampleCubicCatmullRomTexture";
                    break;
            }

            shaderName = IsRW ? shaderName + "RW" : shaderName;
            var shaderClassSource = GetShaderSourceForType<T>(shaderName, TextureDecl.Key, TextureDecl.GetResourceGroupName(context), SamplerDecl.Key, SamplerDecl.GetResourceGroupName(context));

            if (TexCd != null)
            {
                var mixin = shaderClassSource.CreateMixin();
                mixin.AddComposition(TexCd, "TexCd", context, baseKeys);

                if (isSampleLevel && LOD != null)
                    mixin.AddComposition(LOD, "LOD", context, baseKeys);

                return mixin;
            }
            else
                return shaderClassSource;
        }

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return ReturnIfNotNull(TextureDecl, SamplerDecl, TexCd, LOD);
        }
    }
}

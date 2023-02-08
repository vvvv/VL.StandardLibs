using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;
using Stride.Shaders.Compiler;
using System;
using System.Linq;

namespace VL.Stride.Rendering.Materials
{
    public class FallbackMaterial
    {
        Game game;

        public Game Game
        {
            get => game;
            set
            {
                game = value;
            }
        }

        public void Initialize(Game game, MeshRenderFeature meshRenderFeature)
        {
            // Create fallback effect to use when material is still loading
            fallbackMaterialCompiling ??= Material.New(game.GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeShaderClassColor() { MixinReference = "MaterialCompiling" }),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature()
                }
            });

            fallbackMaterialError ??= Material.New(game.GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeShaderClassColor() { MixinReference = "MaterialError" }),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature()
                }
            });

            effectSystem = game.EffectSystem;

            if (meshRenderFeature != null)
                meshRenderFeature.ComputeFallbackEffect += ComputeMeshFallbackEffect;
        }

        MeshRenderFeature meshRenderFeature;

        //[DataMember]

        public MeshRenderFeature MeshRenderFeature
        {
            get => meshRenderFeature;
            set 
            {
                if (meshRenderFeature != null)
                    meshRenderFeature.ComputeFallbackEffect -= ComputeMeshFallbackEffect;

                meshRenderFeature = value;

                if (meshRenderFeature != null)
                    meshRenderFeature.ComputeFallbackEffect += ComputeMeshFallbackEffect;
            }           
        }

        EffectSystem effectSystem;
        Material fallbackMaterialCompiling;
        Material fallbackMaterialError;

        protected Effect ComputeMeshFallbackEffect(RenderObject renderObject, [NotNull] RenderEffect renderEffect, RenderEffectState renderEffectState)
        {
            try
            {
                var renderMesh = (RenderMesh)renderObject;

                // Fallback material that we know something is loading (green glowing FX) or error (red glowing FX)
                var fallbackMaterial = renderEffectState == RenderEffectState.Error ? fallbackMaterialError : fallbackMaterialCompiling;

                // High priority
                var compilerParameters = new CompilerParameters { EffectParameters = { TaskPriority = -1 } };

                // Support skinning
                if (renderMesh.Mesh.Skinning != null && renderMesh.Mesh.Skinning.Bones.Length <= 56)
                {
                    compilerParameters.Set(MaterialKeys.HasSkinningPosition, renderMesh.Mesh.Parameters.Get(MaterialKeys.HasSkinningPosition));
                    compilerParameters.Set(MaterialKeys.HasSkinningNormal, renderMesh.Mesh.Parameters.Get(MaterialKeys.HasSkinningNormal));
                    compilerParameters.Set(MaterialKeys.HasSkinningTangent, renderMesh.Mesh.Parameters.Get(MaterialKeys.HasSkinningTangent));

                    compilerParameters.Set(MaterialKeys.SkinningMaxBones, 56);
                }

                compilerParameters.Set(StrideEffectBaseKeys.HasInstancing, renderMesh.InstanceCount > 0);

                // Set material permutations
                compilerParameters.Set(MaterialKeys.PixelStageSurfaceShaders, fallbackMaterial.Passes[0].Parameters.Get(MaterialKeys.PixelStageSurfaceShaders));
                compilerParameters.Set(MaterialKeys.PixelStageStreamInitializer, fallbackMaterial.Passes[0].Parameters.Get(MaterialKeys.PixelStageStreamInitializer));

                // Set lighting permutations (use custom white light, since this effect will not be processed by the lighting render feature)
                compilerParameters.Set(LightingKeys.EnvironmentLights, new ShaderSourceCollection { new ShaderClassSource("LightConstantWhite") });

                // Initialize parameters with material ones (need a CopyTo?)
                renderEffect.FallbackParameters = new ParameterCollection(renderMesh.MaterialPass.Parameters);

                // Don't show selection wireframe/highlights as compiling
                var ignoreState = renderEffect.EffectSelector.EffectName.EndsWith(".Wireframe") || renderEffect.EffectSelector.EffectName.EndsWith(".Highlight") ||
                                  renderEffect.EffectSelector.EffectName.EndsWith(".Picking");

                
                //if (!ignoreState)
                //{
                //    var meshParams = renderMesh.MaterialPass.Parameters;
                //    var fallbackParams = renderEffect.FallbackParameters;
                //    var textureKey = meshParams.ParameterKeyInfos.Select(pi => pi.Key).OfType<ObjectParameterKey<Texture>>().FirstOrDefault(key => key != MaterialSpecularMicrofacetEnvironmentGGXLUTKeys.EnvironmentLightingDFG_LUT);

                //    if (textureKey != null)
                //    {
                //        fallbackParams.Set(MaterialCompilingKeys.OriginalTexture, meshParams.Get(textureKey));
                //        fallbackParams.Set(MaterialCompilingKeys.HasTexture, true);
                //    }
                //    else
                //    {
                //        var colorKey = meshParams.ParameterKeyInfos.Select(pi => pi.Key).OfType<ValueParameterKey<Color4>>().FirstOrDefault();
                //        if (colorKey != null)
                //        {
                //            fallbackParams.Set(MaterialCompilingKeys.OriginalColor, meshParams.Get(colorKey));
                //        }
                //        else
                //        {
                //            var float4Key = meshParams.ParameterKeyInfos.Select(pi => pi.Key).OfType<ValueParameterKey<Vector4>>().FirstOrDefault();
                //            if (float4Key != null)
                //            {
                //                fallbackParams.Set(MaterialCompilingKeys.OriginalColor, new Color4(meshParams.Get(float4Key)));
                //            }

                //        }

                //        fallbackParams.Set(MaterialCompilingKeys.HasTexture, false);
                //    }

                //}

                if (renderEffectState == RenderEffectState.Error)
                {
                    // Retry every few seconds
                    renderEffect.RetryTime = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                }

                return effectSystem.LoadEffect(renderEffect.EffectSelector.EffectName, compilerParameters).WaitForResult();
            }
            catch
            {
                // TODO: Log or rethrow?
                renderEffect.State = RenderEffectState.Error;
                return null;
            }
        }
    }
}

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Particles.Rendering;
using Stride.Rendering;
using Stride.Rendering.Background;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;
using Stride.Rendering.Images.Dither;
using Stride.Rendering.LightProbes;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Shadows;
using Stride.Rendering.Sprites;
using Stride.Rendering.SubsurfaceScattering;
using Stride.Rendering.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using VL.Core;

namespace VL.Stride.Rendering.Compositing
{
    static class CompositingNodes
    {
        public static IEnumerable<IVLNodeDescription> GetNodeDescriptions(IVLNodeDescriptionFactory nodeFactory)
        {
            var renderingCategory = "Stride.Rendering";
            var renderingCategoryAdvanced = $"{renderingCategory}.Advanced";

            var compositionCategory = $"{renderingCategoryAdvanced}.Compositing";
            var compositionCategoryExperimental = $"{compositionCategory}.Experimental";
            yield return nodeFactory.NewGraphicsRendererNode<GraphicsCompositor>(category: compositionCategory)
                .AddCachedInput(nameof(GraphicsCompositor.Game), x => x.Game, (x, v) => x.Game = v)
                .AddCachedInput(nameof(GraphicsCompositor.SingleView), x => x.SingleView, (x, v) => x.SingleView = v)
                .AddCachedListInput(nameof(GraphicsCompositor.RenderStages), x => x.RenderStages)
                .AddCachedListInput(nameof(GraphicsCompositor.RenderFeatures), x => x.RenderFeatures)
                .AddEnabledPin();

            yield return nodeFactory.NewNode<RenderStage>(category: compositionCategory)
                .AddCachedInput(nameof(RenderStage.Name), x => x.Name, (x, v) => x.Name = v, defaultValue: "RenderStage")
                .AddCachedInput(nameof(RenderStage.EffectSlotName), x => x.EffectSlotName, (x, v) => x.EffectSlotName = v, defaultValue: "Main")
                .AddCachedInput(nameof(RenderStage.SortMode), x => x.SortMode.ToPredefinedSortMode(), (x, v) => x.SortMode = v.ToSortMode())
                .AddCachedInput(nameof(RenderStage.Filter), x => x.Filter, (x, v) => x.Filter = v);

            yield return nodeFactory.NewNode<CustomRenderStageFilter>(category: compositionCategory)
                .AddCachedInput(nameof(CustomRenderStageFilter.IsVisibleFunc), x => x.IsVisibleFunc, (x, v) => x.IsVisibleFunc = v);

            // Render stage selectors
            string renderStageSelectorCategory = $"{compositionCategory}.RenderStageSelector";
            yield return nodeFactory.NewNode<MeshTransparentRenderStageSelector>(category: renderStageSelectorCategory)
                .AddCachedInput(nameof(MeshTransparentRenderStageSelector.EffectName), x => x.EffectName, (x, v) => x.EffectName = v, defaultValue: "StrideForwardShadingEffect")
                .AddCachedInput(nameof(MeshTransparentRenderStageSelector.RenderGroup), x => x.RenderGroup, (x, v) => x.RenderGroup = v, RenderGroupMask.All)
                .AddCachedInput(nameof(MeshTransparentRenderStageSelector.OpaqueRenderStage), x => x.OpaqueRenderStage, (x, v) => x.OpaqueRenderStage = v)
                .AddCachedInput(nameof(MeshTransparentRenderStageSelector.TransparentRenderStage), x => x.TransparentRenderStage, (x, v) => x.TransparentRenderStage = v);

            yield return nodeFactory.NewNode<ShadowMapRenderStageSelector>(category: renderStageSelectorCategory)
                .AddCachedInput(nameof(ShadowMapRenderStageSelector.EffectName), x => x.EffectName, (x, v) => x.EffectName = v, defaultValue: "StrideForwardShadingEffect.ShadowMapCaster")
                .AddCachedInput(nameof(ShadowMapRenderStageSelector.RenderGroup), x => x.RenderGroup, (x, v) => x.RenderGroup = v, RenderGroupMask.All)
                .AddCachedInput(nameof(ShadowMapRenderStageSelector.ShadowMapRenderStage), x => x.ShadowMapRenderStage, (x, v) => x.ShadowMapRenderStage = v);

            yield return nodeFactory.NewNode<SimpleGroupToRenderStageSelector>(category: renderStageSelectorCategory)
                .AddCachedInput(nameof(SimpleGroupToRenderStageSelector.EffectName), x => x.EffectName, (x, v) => x.EffectName = v, defaultValue: "Test")
                .AddCachedInput(nameof(SimpleGroupToRenderStageSelector.RenderGroup), x => x.RenderGroup, (x, v) => x.RenderGroup = v, RenderGroupMask.All)
                .AddCachedInput(nameof(SimpleGroupToRenderStageSelector.RenderStage), x => x.RenderStage, (x, v) => x.RenderStage = v);

            yield return nodeFactory.NewNode<ParticleEmitterTransparentRenderStageSelector>(category: renderStageSelectorCategory)
                .AddCachedInput(nameof(ParticleEmitterTransparentRenderStageSelector.EffectName), x => x.EffectName, (x, v) => x.EffectName = v, defaultValue: "Particles")
                .AddCachedInput(nameof(ParticleEmitterTransparentRenderStageSelector.RenderGroup), x => x.RenderGroup, (x, v) => x.RenderGroup = v, RenderGroupMask.All)
                .AddCachedInput(nameof(ParticleEmitterTransparentRenderStageSelector.OpaqueRenderStage), x => x.OpaqueRenderStage, (x, v) => x.OpaqueRenderStage = v)
                .AddCachedInput(nameof(ParticleEmitterTransparentRenderStageSelector.TransparentRenderStage), x => x.TransparentRenderStage, (x, v) => x.TransparentRenderStage = v);

            yield return nodeFactory.NewNode<SpriteTransparentRenderStageSelector>(category: renderStageSelectorCategory)
                .AddCachedInput(nameof(SpriteTransparentRenderStageSelector.EffectName), x => x.EffectName, (x, v) => x.EffectName = v, defaultValue: "Test")
                .AddCachedInput(nameof(SpriteTransparentRenderStageSelector.RenderGroup), x => x.RenderGroup, (x, v) => x.RenderGroup = v, RenderGroupMask.All)
                .AddCachedInput(nameof(SpriteTransparentRenderStageSelector.OpaqueRenderStage), x => x.OpaqueRenderStage, (x, v) => x.OpaqueRenderStage = v)
                .AddCachedInput(nameof(SpriteTransparentRenderStageSelector.TransparentRenderStage), x => x.TransparentRenderStage, (x, v) => x.TransparentRenderStage = v);

            yield return nodeFactory.NewNode<EntityRendererStageSelector>(category: renderStageSelectorCategory)
                .AddCachedInput(nameof(EntityRendererStageSelector.RenderGroup), x => x.RenderGroup, (x, v) => x.RenderGroup = v, RenderGroupMask.All)
                .AddCachedInput(nameof(EntityRendererStageSelector.BeforeScene), x => x.BeforeScene, (x, v) => x.BeforeScene = v)
                .AddCachedInput(nameof(EntityRendererStageSelector.InSceneOpaque), x => x.InSceneOpaque, (x, v) => x.InSceneOpaque = v)
                .AddCachedInput(nameof(EntityRendererStageSelector.InSceneTransparent), x => x.InSceneTransparent, (x, v) => x.InSceneTransparent = v)
                .AddCachedInput(nameof(EntityRendererStageSelector.AfterScene), x => x.AfterScene, (x, v) => x.AfterScene = v)
                .AddCachedInput(nameof(EntityRendererStageSelector.ShadowCaster), x => x.ShadowCaster, (x, v) => x.ShadowCaster = v)
                .AddCachedInput(nameof(EntityRendererStageSelector.ShadowCasterParaboloid), x => x.ShadowCasterParaboloid, (x, v) => x.ShadowCasterParaboloid = v)
                .AddCachedInput(nameof(EntityRendererStageSelector.ShadowCasterCubeMap), x => x.ShadowCasterCubeMap, (x, v) => x.ShadowCasterCubeMap = v)
                ;

            // Renderers
            yield return nodeFactory.NewGraphicsRendererNode<ClearRenderer>(category: compositionCategory)
                .AddCachedInput(nameof(ClearRenderer.ClearFlags), x => x.ClearFlags, (x, v) => x.ClearFlags = v)
                .AddCachedInput(nameof(ClearRenderer.Color), x => x.Color, (x, v) => x.Color = v, Color4.Black)
                .AddCachedInput(nameof(ClearRenderer.Depth), x => x.Depth, (x, v) => x.Depth = v, 1f)
                .AddCachedInput(nameof(ClearRenderer.Stencil), x => x.Stencil, (x, v) => x.Stencil = v)
                .AddEnabledPin();

            yield return nodeFactory.NewGraphicsRendererNode<DebugRenderer>(category: compositionCategory)
                .AddCachedListInput(nameof(DebugRenderer.DebugRenderStages), x => x.DebugRenderStages)
                .AddEnabledPin();

            yield return nodeFactory.NewGraphicsRendererNode<SingleStageRenderer>(category: compositionCategory)
                .AddCachedInput(nameof(SingleStageRenderer.RenderStage), x => x.RenderStage, (x, v) => x.RenderStage = v)
                .AddEnabledPin();

            yield return nodeFactory.NewGraphicsRendererNode<ForceAspectRatioSceneRenderer>(category: compositionCategory)
                .AddCachedInput(nameof(ForceAspectRatioSceneRenderer.FixedAspectRatio), x => x.FixedAspectRatio, (x, v) => x.FixedAspectRatio = v)
                .AddCachedInput(nameof(ForceAspectRatioSceneRenderer.ForceAspectRatio), x => x.ForceAspectRatio, (x, v) => x.ForceAspectRatio = v)
                .AddCachedInput(nameof(ForceAspectRatioSceneRenderer.Child), x => x.Child, (x, v) => x.Child = v)
                .AddEnabledPin();

            // The CameraRenderer can be found in the EngineNodes due to its dependency on CameraComponent

            yield return nodeFactory.NewGraphicsRendererNode<RenderTextureSceneRenderer>(category: compositionCategory)
                .AddCachedInput(nameof(RenderTextureSceneRenderer.RenderTexture), x => x.RenderTexture, (x, v) => x.RenderTexture = v)
                .AddCachedInput(nameof(RenderTextureSceneRenderer.Child), x => x.Child, (x, v) => x.Child = v)
                .AddEnabledPin();

            yield return nodeFactory.NewGraphicsRendererNode<SceneRendererCollection>(category: compositionCategory)
                .AddCachedListInput(nameof(SceneRendererCollection.Children), x => x.Children)
                .AddEnabledPin();

            var defaultResolver = new MSAAResolver();
            yield return nodeFactory.NewGraphicsRendererNode<ForwardRenderer>(category: compositionCategory, copyOnWrite: true)
                .AddCachedInput(nameof(ForwardRenderer.Clear), x => x.Clear, (x, v) => x.Clear = v, defaultValue: null /* We want null as default */)
                .AddCachedInput(nameof(ForwardRenderer.OpaqueRenderStage), x => x.OpaqueRenderStage, (x, v) => x.OpaqueRenderStage = v)
                .AddCachedInput(nameof(ForwardRenderer.TransparentRenderStage), x => x.TransparentRenderStage, (x, v) => x.TransparentRenderStage = v)
                .AddCachedListInput(nameof(ForwardRenderer.ShadowMapRenderStages), x => x.ShadowMapRenderStages)
                .AddCachedInput(nameof(ForwardRenderer.GBufferRenderStage), x => x.GBufferRenderStage, (x, v) => x.GBufferRenderStage = v)
                .AddCachedInput(nameof(ForwardRenderer.PostEffects), x => x.PostEffects, (x, v) => x.PostEffects = v)
                .AddCachedInput(nameof(ForwardRenderer.LightShafts), x => x.LightShafts, (x, v) => x.LightShafts = v)
                .AddCachedInput(nameof(ForwardRenderer.VRSettings), x => x.VRSettings, (x, v) => x.VRSettings = v)
                .AddCachedInput(nameof(ForwardRenderer.SubsurfaceScatteringBlurEffect), x => x.SubsurfaceScatteringBlurEffect, (x, v) => x.SubsurfaceScatteringBlurEffect = v)
                .AddCachedInput(nameof(ForwardRenderer.MSAALevel), x => x.MSAALevel, (x, v) => x.MSAALevel = v)
                .AddCachedInput(nameof(ForwardRenderer.MSAAResolver), x => x.MSAAResolver, (x, v) =>
                {
                    var s = x.MSAAResolver;
                    var y = v ?? defaultResolver;
                    s.FilterType = y.FilterType;
                    s.FilterRadius = y.FilterRadius;
                })
                .AddCachedInput(nameof(ForwardRenderer.BindDepthAsResourceDuringTransparentRendering), x => x.BindDepthAsResourceDuringTransparentRendering, (x, v) => x.BindDepthAsResourceDuringTransparentRendering = v)
                .AddEnabledPin();

            yield return nodeFactory.NewNode<ViewportView>(category: compositionCategoryExperimental, copyOnWrite: false)
                .AddInput(nameof(ViewportView.View), x => x.View, (x, v) => x.View = v)
                .AddInput(nameof(ViewportView.Viewport), x => x.Viewport, (x, v) => x.Viewport = v)
                .AddInput(nameof(ViewportView.Renderer), x => x.Renderer, (x, v) => x.Renderer = v);

            yield return new StrideNodeDesc<ViewportSettings>(nodeFactory, category: compositionCategoryExperimental) { CopyOnWrite = false };

            yield return nodeFactory.NewNode<ViewportRenderInfo>(category: compositionCategoryExperimental)
                .AddOutput(nameof(ViewportRenderInfo.CameraComponent), x => x.CameraComponent)
                .AddOutput(nameof(ViewportRenderInfo.RenderTargetSize), x => x.RenderTargetSize);

            //Same as the Stride ForwardRenderer class, but with additional ViewportSettings that work similar to the VRSettings.
            yield return nodeFactory.NewGraphicsRendererNode<VLForwardRenderer>(category: compositionCategoryExperimental, copyOnWrite: true)
                .AddCachedInput(nameof(VLForwardRenderer.Clear), x => x.Clear, (x, v) => x.Clear = v, defaultValue: null /* We want null as default */)
                .AddCachedInput(nameof(VLForwardRenderer.OpaqueRenderStage), x => x.OpaqueRenderStage, (x, v) => x.OpaqueRenderStage = v)
                .AddCachedInput(nameof(VLForwardRenderer.TransparentRenderStage), x => x.TransparentRenderStage, (x, v) => x.TransparentRenderStage = v)
                .AddCachedListInput(nameof(VLForwardRenderer.ShadowMapRenderStages), x => x.ShadowMapRenderStages)
                .AddCachedInput(nameof(VLForwardRenderer.GBufferRenderStage), x => x.GBufferRenderStage, (x, v) => x.GBufferRenderStage = v)
                .AddCachedInput(nameof(VLForwardRenderer.PostEffects), x => x.PostEffects, (x, v) => x.PostEffects = v)
                .AddCachedInput(nameof(VLForwardRenderer.LightShafts), x => x.LightShafts, (x, v) => x.LightShafts = v)
                .AddCachedInput(nameof(VLForwardRenderer.VRSettings), x => x.VRSettings, (x, v) => x.VRSettings = v)
                .AddCachedInput(nameof(VLForwardRenderer.ViewportSettings), x => x.ViewportSettings, (x, v) => x.ViewportSettings = v)
                .AddCachedInput(nameof(VLForwardRenderer.SubsurfaceScatteringBlurEffect), x => x.SubsurfaceScatteringBlurEffect, (x, v) => x.SubsurfaceScatteringBlurEffect = v)
                .AddCachedInput(nameof(VLForwardRenderer.MSAALevel), x => x.MSAALevel, (x, v) => x.MSAALevel = v)
                .AddCachedInput(nameof(VLForwardRenderer.MSAAResolver), x => x.MSAAResolver, (x, v) =>
                {
                    var s = x.MSAAResolver;
                    var y = v ?? defaultResolver;
                    s.FilterType = y.FilterType;
                    s.FilterRadius = y.FilterRadius;
                })
                .AddCachedInput(nameof(VLForwardRenderer.BindDepthAsResourceDuringTransparentRendering), x => x.BindDepthAsResourceDuringTransparentRendering, (x, v) => x.BindDepthAsResourceDuringTransparentRendering = v)
                .AddEnabledPin();

            yield return new StrideNodeDesc<SubsurfaceScatteringBlur>(nodeFactory, category: compositionCategory);
            yield return new StrideNodeDesc<LightShafts>(nodeFactory, category: compositionCategory);
            yield return new StrideNodeDesc<MSAAResolver>(nodeFactory, category: compositionCategory);

            // Post processing
            var postFxCategory = $"{renderingCategory}.PostFX";
            yield return CreatePostEffectsNode();

            yield return new StrideNodeDesc<AmbientOcclusion>(nodeFactory, category: postFxCategory);
            yield return new StrideNodeDesc<LocalReflections>(nodeFactory, category: postFxCategory);
            yield return new StrideNodeDesc<DepthOfField>(nodeFactory, category: postFxCategory);
            yield return nodeFactory.NewNode<Fog>(category: postFxCategory, copyOnWrite: true)
                .AddCachedInput(nameof(Fog.Density), x => x.Density * 10, (x, v) => x.Density = v * 0.1f, 1f)
                .AddCachedInput(nameof(Fog.Color), x => x.Color.ToColor4(), (x, v) => x.Color = v.ToColor3(), Color4.White)
                .AddCachedInput(nameof(Fog.FogStart), x => x.FogStart, (x, v) => x.FogStart = v, 3)
                .AddCachedInput(nameof(Fog.SkipBackground), x => x.SkipBackground, (x, v) => x.SkipBackground = v, false)
                .AddCachedInput(nameof(Fog.Enabled), x => x.Enabled, (x, v) => x.Enabled = v, true);
            yield return new StrideNodeDesc<Outline>(nodeFactory, category: postFxCategory);
            yield return new StrideNodeDesc<BrightFilter>(nodeFactory, category: postFxCategory);
            yield return new StrideNodeDesc<Bloom>(nodeFactory, category: postFxCategory);
            yield return new StrideNodeDesc<LightStreak>(nodeFactory, category: postFxCategory);
            yield return new StrideNodeDesc<LensFlare>(nodeFactory, category: postFxCategory);
            // AA
            yield return new StrideNodeDesc<FXAAEffect>(nodeFactory, category: postFxCategory);
            yield return new StrideNodeDesc<TemporalAntiAliasEffect>(nodeFactory, category: postFxCategory);
            // Color transforms
            var colorTransformsCategory = $"{postFxCategory}.ColorTransforms";
            yield return new StrideNodeDesc<LuminanceToChannelTransform>(nodeFactory, category: colorTransformsCategory) { CopyOnWrite = false };
            yield return new StrideNodeDesc<FilmGrain>(nodeFactory, category: colorTransformsCategory) { CopyOnWrite = false };
            yield return nodeFactory.NewNode<Vignetting>(category: colorTransformsCategory, copyOnWrite: false)
                .AddCachedInput(nameof(Vignetting.Amount), x => x.Amount, (x, v) => x.Amount = v, 0.8f)
                .AddCachedInput(nameof(Vignetting.Radius), x => x.Radius, (x, v) => x.Radius = v, 0.7f)
                .AddCachedInput(nameof(Vignetting.Color), x => x.Color.ToColor4(), (x, v) => x.Color = v.ToColor3(), Color4.Black)
                .AddCachedInput(nameof(Vignetting.Enabled), x => x.Enabled, (x, v) => x.Enabled = v, true);
            yield return new StrideNodeDesc<Dither>(nodeFactory, category: colorTransformsCategory) { CopyOnWrite = false };

            yield return nodeFactory.NewNode<ToneMap>(category: colorTransformsCategory, copyOnWrite: true)
                .AddCachedInput(nameof(ToneMap.Operator), x => x.Operator, (x, v) =>
                {
                    if (v != null && v != x.Operator && x.Group != null)
                    {
                        // For same operator types the shader permutation of the whole color transform group stays the same
                        // and therefor Stride will not update the parameter copiers of the transforms.
                        // We therefor invalidate the shader manually here.
                        var transformGroupEffectField = typeof(ColorTransformGroup).GetField("transformGroupEffect", BindingFlags.Instance | BindingFlags.NonPublic);
                        var transformGroupEffect = (ImageEffectShader)transformGroupEffectField.GetValue(x.Group);
                        var descriptorReflectionField = typeof(EffectInstance).GetField("descriptorReflection", BindingFlags.Instance | BindingFlags.NonPublic);
                        descriptorReflectionField.SetValue(transformGroupEffect.EffectInstance, null);
                    }
                    x.Operator = v;
                })
                .AddCachedInput(nameof(ToneMap.AutoKeyValue), x => x.AutoKeyValue, (x, v) => x.AutoKeyValue = v, true)
                .AddCachedInput(nameof(ToneMap.KeyValue), x => x.KeyValue, (x, v) => x.KeyValue = v, 0.18f)
                .AddCachedInput(nameof(ToneMap.AutoExposure), x => x.AutoExposure, (x, v) => x.AutoExposure = v, true)
                .AddCachedInput(nameof(ToneMap.Exposure), x => x.Exposure, (x, v) => x.Exposure = v, 0f)
                .AddCachedInput(nameof(ToneMap.TemporalAdaptation), x => x.TemporalAdaptation, (x, v) => x.TemporalAdaptation = v, true)
                .AddCachedInput(nameof(ToneMap.AdaptationRate), x => x.AdaptationRate, (x, v) => x.AdaptationRate = v, 1.0f)
                .AddCachedInput(nameof(ToneMap.UseLocalLuminance), x => x.UseLocalLuminance, (x, v) => x.UseLocalLuminance = v, false)
                .AddCachedInput(nameof(ToneMap.LuminanceLocalFactor), x => x.LuminanceLocalFactor, (x, v) => x.LuminanceLocalFactor = v, 0.0f)
                .AddCachedInput(nameof(ToneMap.Contrast), x => x.Contrast, (x, v) => x.Contrast = v, 0f)
                .AddCachedInput(nameof(ToneMap.Brightness), x => x.Brightness, (x, v) => x.Brightness = v, 0f)
                .AddCachedInput(nameof(ToneMap.Enabled), x => x.Enabled, (x, v) => x.Enabled = v, true);

            //yield return new StrideNodeDesc<ToneMap>(nodeFactory, category: colorTransformsCategory) { CopyOnWrite = false };

            var operatorsCategory = $"{colorTransformsCategory}.ToneMapOperators";
            yield return new StrideNodeDesc<ToneMapHejl2Operator>(nodeFactory, "Hejl2", category: operatorsCategory) { CopyOnWrite = false };
            yield return new StrideNodeDesc<ToneMapHejlDawsonOperator>(nodeFactory, "HejlDawson", category: operatorsCategory) { CopyOnWrite = false };
            yield return new StrideNodeDesc<ToneMapMikeDayOperator>(nodeFactory, "MikeDay", category: operatorsCategory) { CopyOnWrite = false };
            yield return new StrideNodeDesc<ToneMapU2FilmicOperator>(nodeFactory, "U2Filmic", category: operatorsCategory) { CopyOnWrite = false };
            yield return new StrideNodeDesc<ToneMapDragoOperator>(nodeFactory, "Drago", category: operatorsCategory) { CopyOnWrite = false };
            yield return new StrideNodeDesc<ToneMapExponentialOperator>(nodeFactory, "Exponential", category: operatorsCategory) { CopyOnWrite = false };
            yield return new StrideNodeDesc<ToneMapLogarithmicOperator>(nodeFactory, "Logarithmic", category: operatorsCategory) { CopyOnWrite = false };
            yield return new StrideNodeDesc<ToneMapReinhardOperator>(nodeFactory, "Reinhard", category: operatorsCategory) { CopyOnWrite = false };
            yield return new StrideNodeDesc<ToneMapACESOperator>(nodeFactory, "ACES", category: operatorsCategory) { CopyOnWrite = false };

            // Root render features
            yield return nodeFactory.NewNode<MeshRenderFeature>(category: renderingCategoryAdvanced)
                .AddCachedListInput(nameof(MeshRenderFeature.RenderFeatures), x => x.RenderFeatures)
                .AddCachedListInput(nameof(MeshRenderFeature.RenderStageSelectors), x => x.RenderStageSelectors)
                .AddCachedListInput(nameof(MeshRenderFeature.PipelineProcessors), x => x.PipelineProcessors);

            yield return nodeFactory.NewNode<BackgroundRenderFeature>(category: renderingCategoryAdvanced)
                .AddCachedListInput(nameof(BackgroundRenderFeature.RenderStageSelectors), x => x.RenderStageSelectors);

            yield return nodeFactory.NewNode<SpriteRenderFeature>(category: renderingCategoryAdvanced)
                .AddCachedListInput(nameof(SpriteRenderFeature.RenderStageSelectors), x => x.RenderStageSelectors);

            yield return nodeFactory.NewNode<EntityRendererRenderFeature>(category: renderingCategoryAdvanced)
                .AddCachedListInput(nameof(EntityRendererRenderFeature.RenderStageSelectors), x => x.RenderStageSelectors)
                .AddCachedInput(nameof(EntityRendererRenderFeature.HelpersRenderStage), x => x.HelpersRenderStage, (x, v) => x.HelpersRenderStage = v)
                .AddCachedInput(nameof(EntityRendererRenderFeature.HelpersRenderer), x => x.HelpersRenderer, (x, v) => x.HelpersRenderer = v);

            yield return nodeFactory.NewNode<UIRenderFeature>(category: renderingCategoryAdvanced)
                .AddCachedListInput(nameof(UIRenderFeature.RenderStageSelectors), x => x.RenderStageSelectors);

            // Sub render features for mesh render feature
            var renderFeaturesCategory = $"{renderingCategoryAdvanced}.RenderFeatures";
            yield return new StrideNodeDesc<TransformRenderFeature>(nodeFactory, category: renderFeaturesCategory);
            yield return new StrideNodeDesc<SkinningRenderFeature>(nodeFactory, category: renderFeaturesCategory);
            yield return new StrideNodeDesc<MaterialRenderFeature>(nodeFactory, category: renderFeaturesCategory);
            yield return new StrideNodeDesc<ShadowCasterRenderFeature>(nodeFactory, category: renderFeaturesCategory);
            yield return new StrideNodeDesc<InstancingRenderFeature>(nodeFactory, category: renderFeaturesCategory);
            yield return new StrideNodeDesc<SubsurfaceScatteringRenderFeature>(nodeFactory, category: renderFeaturesCategory);
            yield return new StrideNodeDesc<VLEffectRenderFeature>(nodeFactory, category: renderFeaturesCategory);

            yield return nodeFactory.NewNode<ForwardLightingRenderFeature>(category: renderFeaturesCategory)
                .AddCachedListInput(nameof(ForwardLightingRenderFeature.LightRenderers), x => x.LightRenderers)
                .AddCachedInput(nameof(ForwardLightingRenderFeature.ShadowMapRenderer), x => x.ShadowMapRenderer, (x, v) => x.ShadowMapRenderer = v);

            var pipelineProcessorsCategory = $"{renderingCategoryAdvanced}.PipelineProcessors";
            yield return nodeFactory.NewNode<MeshPipelineProcessor>(category: pipelineProcessorsCategory)
                .AddCachedInput(nameof(MeshPipelineProcessor.TransparentRenderStage), x => x.TransparentRenderStage, (x, v) => x.TransparentRenderStage = v);

            yield return nodeFactory.NewNode<ShadowMeshPipelineProcessor>(category: pipelineProcessorsCategory)
                .AddCachedInput(nameof(ShadowMeshPipelineProcessor.ShadowMapRenderStage), x => x.ShadowMapRenderStage, (x, v) => x.ShadowMapRenderStage = v)
                .AddCachedInput(nameof(ShadowMeshPipelineProcessor.DepthClipping), x => x.DepthClipping, (x, v) => x.DepthClipping = v);

            yield return nodeFactory.NewNode<WireframePipelineProcessor>(category: pipelineProcessorsCategory)
                .AddCachedInput(nameof(WireframePipelineProcessor.RenderStage), x => x.RenderStage, (x, v) => x.RenderStage = v);

            // Light renderers - make enum
            var lightsCategory = $"{renderingCategoryAdvanced}.Light";
            yield return new StrideNodeDesc<LightAmbientRenderer>(nodeFactory, category: lightsCategory);
            yield return new StrideNodeDesc<LightSkyboxRenderer>(nodeFactory, category: lightsCategory);
            yield return new StrideNodeDesc<LightDirectionalGroupRenderer>(nodeFactory, category: lightsCategory);
            yield return new StrideNodeDesc<LightPointGroupRenderer>(nodeFactory, category: lightsCategory);
            yield return new StrideNodeDesc<LightSpotGroupRenderer>(nodeFactory, category: lightsCategory);
            yield return new StrideNodeDesc<LightClusteredPointSpotGroupRenderer>(nodeFactory, category: lightsCategory);
            yield return new StrideNodeDesc<LightProbeRenderer>(nodeFactory, category: lightsCategory);

            // Shadow map renderers
            var shadowsCategory = $"{renderingCategoryAdvanced}.Shadow";
            yield return nodeFactory.NewNode<ShadowMapRenderer>(category: shadowsCategory)
                .AddCachedListInput(nameof(ShadowMapRenderer.Renderers), x => x.Renderers);

            yield return new StrideNodeDesc<LightDirectionalShadowMapRenderer>(nodeFactory, category: shadowsCategory);
            yield return new StrideNodeDesc<LightSpotShadowMapRenderer>(nodeFactory, category: shadowsCategory);
            yield return new StrideNodeDesc<LightPointShadowMapRendererParaboloid>(nodeFactory, category: shadowsCategory);
            yield return new StrideNodeDesc<LightPointShadowMapRendererCubeMap>(nodeFactory, category: shadowsCategory);

            IVLNodeDescription CreatePostEffectsNode()
            {
                return nodeFactory.NewNode<PostProcessingEffects>(name: "PostFXCore (Internal)", category: renderingCategory, copyOnWrite: false, 
                    init: effects =>
                    {
                        // Can't use effects.DisableAll() - disables private effects used by AA
                        effects.Fog.Enabled = false;
                        effects.Outline.Enabled = false;
                        effects.AmbientOcclusion.Enabled = false;
                        effects.LocalReflections.Enabled = false;
                        effects.DepthOfField.Enabled = false;
                        effects.BrightFilter.Enabled = false;
                        effects.Bloom.Enabled = false;
                        effects.LightStreak.Enabled = false;
                        effects.LensFlare.Enabled = false;
                        // ColorTransforms delegates to an empty list, keep it enabled
                        effects.ColorTransforms.Enabled = true;
                        effects.Antialiasing.Enabled = false;
                    })
                    .AddCachedInput(nameof(PostProcessingEffects.AmbientOcclusion), x => x.AmbientOcclusion, (x, v) =>
                    {
                        var s = x.AmbientOcclusion;
                        if (v != null)
                        {
                            s.Enabled = v.Enabled;
                            s.NumberOfSamples = v.NumberOfSamples;
                            s.ParamProjScale = v.ParamProjScale;
                            s.ParamIntensity = v.ParamIntensity;
                            s.ParamBias = v.ParamBias;
                            s.ParamRadius = v.ParamRadius;
                            s.NumberOfBounces = v.NumberOfBounces;
                            s.BlurScale = v.BlurScale;
                            s.EdgeSharpness = v.EdgeSharpness;
                            s.TempSize = v.TempSize;
                        }
                        else
                        {
                            s.Enabled = false;
                        }
                    }, defaultValue: null /* null is used to disable */)
                    .AddCachedInput(nameof(PostProcessingEffects.LocalReflections), x => x.LocalReflections, (x, v) =>
                    {
                        var s = x.LocalReflections;
                        if (v != null)
                        {
                            s.Enabled = v.Enabled;
                            s.DepthResolution = v.DepthResolution;
                            s.RayTracePassResolution = v.RayTracePassResolution;
                            s.MaxSteps = v.MaxSteps;
                            s.BRDFBias = v.BRDFBias;
                            s.GlossinessThreshold = v.GlossinessThreshold;
                            s.WorldAntiSelfOcclusionBias = v.WorldAntiSelfOcclusionBias;
                            s.ResolvePassResolution = v.ResolvePassResolution;
                            s.ResolveSamples = v.ResolveSamples;
                            s.ReduceHighlights = v.ReduceHighlights;
                            s.EdgeFadeFactor = v.EdgeFadeFactor;
                            s.UseColorBufferMips = v.UseColorBufferMips;
                            s.TemporalEffect = v.TemporalEffect;
                            s.TemporalScale = v.TemporalScale;
                            s.TemporalResponse = v.TemporalResponse;
                        }
                        else
                        {
                            s.Enabled = false;
                        }
                    }, defaultValue: null /* null is used to disable */)
                    .AddCachedInput(nameof(PostProcessingEffects.DepthOfField), x => x.DepthOfField, (x, v) =>
                    {
                        var s = x.DepthOfField;
                        if (v != null)
                        {
                            s.Enabled = v.Enabled;
                            s.MaxBokehSize = v.MaxBokehSize;
                            s.DOFAreas = v.DOFAreas;
                            s.QualityPreset = v.QualityPreset;
                            s.Technique = v.Technique;
                            s.AutoFocus = v.AutoFocus;
                        }
                        else
                        {
                            s.Enabled = false;
                        }
                    }, defaultValue: null /* null is used to disable */)
                    .AddCachedInput(nameof(PostProcessingEffects.Fog), x => x.Fog, (x, v) =>
                    {
                        var s = x.Fog;
                        if (v != null)
                        {
                            s.Enabled = v.Enabled;
                            s.Density = v.Density;
                            s.Color = v.Color;
                            s.FogStart = v.FogStart;
                        }
                        else
                        {
                            s.Enabled = false;
                        }
                    }, defaultValue: null /* null is used to disable */)
                    .AddCachedInput(nameof(PostProcessingEffects.Outline), x => x.Outline, (x, v) =>
                    {
                        var s = x.Outline;
                        if (v != null)
                        {
                            s.Enabled = v.Enabled;
                            s.NormalWeight = v.NormalWeight;
                            s.DepthWeight = v.DepthWeight;
                        }
                        else
                        {
                            s.Enabled = false;
                        }
                    }, defaultValue: null /* null is used to disable */)
                    .AddCachedInput(nameof(PostProcessingEffects.BrightFilter), x => x.BrightFilter, (x, v) =>
                    {
                        var s = x.BrightFilter;
                        if (v != null)
                        {
                            s.Enabled = v.Enabled;
                            s.Threshold = v.Threshold;
                            s.Steepness = v.Steepness;
                            s.Color = v.Color;
                        }
                        else
                        {
                            // Keep the bright filter enabled. Needed by Bloom, LightStreak and LensFlare. 
                            // Stride will only use it if one of those is enabled.
                            s.Enabled = true;
                        }
                    }, defaultValue: null /* null is used to disable */)
                    .AddCachedInput(nameof(PostProcessingEffects.Bloom), x => x.Bloom, (x, v) =>
                    {
                        var s = x.Bloom;
                        if (v != null)
                        {
                            s.Enabled = v.Enabled;
                            s.Radius = v.Radius;
                            s.Amount = v.Amount;
                            s.DownScale = v.DownScale;
                            s.SigmaRatio = v.SigmaRatio;
                            s.Distortion = v.Distortion;
                            s.Afterimage.Enabled = v.Afterimage.Enabled;
                            s.Afterimage.FadeOutSpeed = v.Afterimage.FadeOutSpeed;
                            s.Afterimage.Sensitivity = v.Afterimage.Sensitivity;
                        }
                        else
                        {
                            s.Enabled = false;
                        }
                    }, defaultValue: null /* null is used to disable */)
                    .AddCachedInput(nameof(PostProcessingEffects.LightStreak), x => x.LightStreak, (x, v) =>
                    {
                        var s = x.LightStreak;
                        if (v != null)
                        {
                            s.Enabled = v.Enabled;
                            s.Amount = v.Amount;
                            s.StreakCount = v.StreakCount;
                            s.Attenuation = v.Attenuation;
                            s.Phase = v.Phase;
                            s.ColorAberrationStrength = v.ColorAberrationStrength;
                            s.IsAnamorphic = v.IsAnamorphic;
                        }
                        else
                        {
                            s.Enabled = false;
                        }
                    }, defaultValue: null /* null is used to disable */)
                    .AddCachedInput(nameof(PostProcessingEffects.LensFlare), x => x.LensFlare, (x, v) =>
                    {
                        var s = x.LensFlare;
                        if (v != null)
                        {
                            s.Enabled = v.Enabled;
                            s.Amount = v.Amount;
                            s.ColorAberrationStrength = v.ColorAberrationStrength;
                            s.HaloFactor = v.HaloFactor;
                        }
                        else
                        {
                            s.Enabled = false;
                        }
                    }, defaultValue: null /* null is used to disable */)
                    .AddCachedListInput(nameof(PostProcessingEffects.ColorTransforms), x => x.ColorTransforms.Transforms)
                    .AddCachedInput(nameof(PostProcessingEffects.Antialiasing), x => x.Antialiasing, (x, v) => x.Antialiasing = v);
            }
        }

        internal static CustomNodeDesc<TRenderer> NewGraphicsRendererNode<TRenderer>(this IVLNodeDescriptionFactory factory, string category, string name = null, bool copyOnWrite = false)
            where TRenderer : class, IGraphicsRenderer, new()
        {
            return factory.NewNode<TRenderer>(name: name, category: category, copyOnWrite: copyOnWrite);
        }

        internal static CustomNodeDesc<TRenderer> AddEnabledPin<TRenderer>(this CustomNodeDesc<TRenderer> node)
            where TRenderer : class, IGraphicsRenderer
        {
            return node.AddCachedInput(nameof(IGraphicsRenderer.Enabled), x => x.Enabled, (x, v) => x.Enabled = v, defaultValue: true);
        }
    }
}

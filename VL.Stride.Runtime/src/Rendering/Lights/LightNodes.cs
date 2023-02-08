using Stride.Core.Mathematics;
using Stride.Rendering.Colors;
using Stride.Rendering.Lights;
using System.Collections.Generic;
using VL.Core;

namespace VL.Stride.Rendering.Lights
{
    static partial class LightNodes
    {
        public static IEnumerable<IVLNodeDescription> GetNodeDescriptions(IVLNodeDescriptionFactory factory)
        {
            var baseCategory = "Stride.Lights.Advanced";
            var lightTypesCategory = $"{baseCategory}.LightTypes";
            var shadowMapsCategory = $"{baseCategory}.ShadowMaps";

            yield return NewColorLightNode<LightAmbient>(factory, lightTypesCategory);

            yield return NewEnvironmentLightNode<LightSkybox>(factory, lightTypesCategory)
                .AddCachedInput(nameof(LightSkybox.Skybox), x => x.Skybox, (x, v) => x.Skybox = v);

            yield return NewSkyboxNode(factory, lightTypesCategory);

            yield return NewDirectLightNode<LightDirectional>(factory, lightTypesCategory);

            yield return NewDirectLightNode<LightPoint>(factory, lightTypesCategory)
                .AddCachedInput(nameof(LightPoint.Radius), x => x.Radius, (x, v) => x.Radius = v, 5f);

            yield return NewDirectLightNode<LightSpot>(factory, lightTypesCategory)
                .AddCachedInput(nameof(LightSpot.Range), x => x.Range, (x, v) => x.Range = v, 5f)
                .AddCachedInput(nameof(LightSpot.AngleInner), x => x.AngleInner, (x, v) => x.AngleInner = v, 30f)
                .AddCachedInput(nameof(LightSpot.AngleOuter), x => x.AngleOuter, (x, v) => x.AngleOuter = v, 35f)
                .AddCachedInput(nameof(LightSpot.ProjectiveTexture), x => x.ProjectiveTexture, (x, v) => x.ProjectiveTexture = v)
                .AddCachedInput(nameof(LightSpot.UVScale), x => x.UVScale, (x, v) => x.UVScale = v, Vector2.One)
                .AddCachedInput(nameof(LightSpot.UVOffset), x => x.UVOffset, (x, v) => x.UVOffset = v, Vector2.Zero)
                .AddCachedInput(nameof(LightSpot.MipMapScale), x => x.MipMapScale, (x, v) => x.MipMapScale = v, 0f)
                .AddCachedInput(nameof(LightSpot.AspectRatio), x => x.AspectRatio, (x, v) => x.AspectRatio = v, 1f)
                .AddCachedInput(nameof(LightSpot.TransitionArea), x => x.TransitionArea, (x, v) => x.TransitionArea = v, 0.2f)
                .AddCachedInput(nameof(LightSpot.ProjectionPlaneDistance), x => x.ProjectionPlaneDistance, (x, v) => x.ProjectionPlaneDistance = v, 1f)
                .AddCachedInput(nameof(LightSpot.FlipMode), x => x.FlipMode, (x, v) => x.FlipMode = v);

            yield return NewShadowNode<LightDirectionalShadowMap>(factory, shadowMapsCategory)
                .AddCachedInput(nameof(LightDirectionalShadowMap.CascadeCount), x => x.CascadeCount, (x, v) => x.CascadeCount = v, LightShadowMapCascadeCount.FourCascades)
                .AddCachedInput(nameof(LightDirectionalShadowMap.DepthRange), x => x.DepthRange, (x, v) =>
                {
                    var s = x.DepthRange;
                    s.IsAutomatic = v.IsAutomatic;
                    s.ManualMinDistance = v.ManualMinDistance;
                    s.ManualMaxDistance = v.ManualMaxDistance;
                    s.IsBlendingCascades = v.IsBlendingCascades;
                })
                .AddCachedInput(nameof(LightDirectionalShadowMap.PartitionMode), x => x.PartitionMode, (x, v) => x.PartitionMode = v)
                .AddCachedInput(nameof(LightDirectionalShadowMap.StabilizationMode), x => x.StabilizationMode, (x, v) => x.StabilizationMode = v, LightShadowMapStabilizationMode.ProjectionSnapping)
                .AddDefaultPins()
                .AddEnabledPin();

            yield return NewShadowNode<LightPointShadowMap>(factory, shadowMapsCategory)
                .AddCachedInput(nameof(LightPointShadowMap.Type), x => x.Type, (x, v) => x.Type = v, LightPointShadowMapType.CubeMap)
                .AddDefaultPins()
                .AddEnabledPin();

            yield return NewShadowNode<LightStandardShadowMap>(factory, shadowMapsCategory)
                .AddDefaultPins()
                .AddEnabledPin();

            yield return factory.NewNode<LightShadowMapFilterTypePcf>(category: shadowMapsCategory, copyOnWrite: false)
                .AddCachedInput(nameof(LightShadowMapFilterTypePcf.FilterSize), x => x.FilterSize, (x, v) => x.FilterSize = v, LightShadowMapFilterTypePcfSize.Filter5x5);

            yield return factory.NewNode<LightDirectionalShadowMap.DepthRangeParameters>(category: shadowMapsCategory)
                .AddCachedInput(nameof(LightDirectionalShadowMap.DepthRangeParameters.IsAutomatic), x => x.IsAutomatic, (x, v) => x.IsAutomatic = v, true)
                .AddCachedInput(nameof(LightDirectionalShadowMap.DepthRangeParameters.ManualMinDistance), x => x.ManualMinDistance, (x, v) => x.ManualMinDistance = v, LightDirectionalShadowMap.DepthRangeParameters.DefaultMinDistance)
                .AddCachedInput(nameof(LightDirectionalShadowMap.DepthRangeParameters.ManualMaxDistance), x => x.ManualMaxDistance, (x, v) => x.ManualMaxDistance = v, LightDirectionalShadowMap.DepthRangeParameters.DefaultMaxDistance)
                .AddCachedInput(nameof(LightDirectionalShadowMap.DepthRangeParameters.IsBlendingCascades), x => x.IsBlendingCascades, (x, v) => x.IsBlendingCascades = v, true);

            yield return factory.NewNode<LightDirectionalShadowMap.PartitionManual>(category: shadowMapsCategory)
                .AddCachedInput(nameof(LightDirectionalShadowMap.PartitionManual.SplitDistance0), x => x.SplitDistance0, (x, v) => x.SplitDistance0 = v, 0.05f)
                .AddCachedInput(nameof(LightDirectionalShadowMap.PartitionManual.SplitDistance1), x => x.SplitDistance1, (x, v) => x.SplitDistance1 = v, 0.15f)
                .AddCachedInput(nameof(LightDirectionalShadowMap.PartitionManual.SplitDistance2), x => x.SplitDistance2, (x, v) => x.SplitDistance2 = v, 0.50f)
                .AddCachedInput(nameof(LightDirectionalShadowMap.PartitionManual.SplitDistance3), x => x.SplitDistance3, (x, v) => x.SplitDistance3 = v, 1.00f);

            yield return factory.NewNode<LightDirectionalShadowMap.PartitionLogarithmic>(category: shadowMapsCategory)
                .AddCachedInput(nameof(LightDirectionalShadowMap.PartitionLogarithmic.PSSMFactor), x => x.PSSMFactor, (x, v) => x.PSSMFactor = v, 0.5f);
        }

        static CustomNodeDesc<TLight> NewEnvironmentLightNode<TLight>(IVLNodeDescriptionFactory factory, string category)
            where TLight : class, IEnvironmentLight, new()
        {
            return factory.NewNode<TLight>(category: category, copyOnWrite: false);
        }

        static CustomNodeDesc<TLight> NewColorLightNode<TLight>(IVLNodeDescriptionFactory factory, string category)
            where TLight : class, IColorLight, new()
        {
            return factory.NewNode<TLight>(category: category, copyOnWrite: false)
                .AddCachedInput(nameof(IColorLight.Color), x => new Color4(x.Color.ComputeColor()), (x, v) => ((ColorRgbProvider)x.Color).Value = v.ToColor3(), Color4.White);
        }

        static CustomNodeDesc<TLight> NewDirectLightNode<TLight>(IVLNodeDescriptionFactory factory, string category)
            where TLight : class, IColorLight, IDirectLight, new()
        {
            return NewColorLightNode<TLight>(factory, category)
                .AddCachedInput(
                    name: nameof(IDirectLight.Shadow),
                    getter: x => x.Shadow, 
                    setter: (x, v) =>
                    {
                        var s = x.Shadow;
                        if (v != null)
                        {
                            s.Enabled = v.Enabled;
                            s.Filter = v.Filter;
                            s.Size = v.Size;
                            s.BiasParameters.DepthBias = v.BiasParameters.DepthBias;
                            s.BiasParameters.NormalOffsetScale = v.BiasParameters.NormalOffsetScale;
                            s.Debug = v.Debug;
                        }
                        else
                        {
                            s.Enabled = false;
                        }
                    },
                    defaultValue: null /* null disables the shadow */);
        }

        static CustomNodeDesc<TShadow> NewShadowNode<TShadow>(IVLNodeDescriptionFactory factory, string category)
            where TShadow : LightShadowMap, new()
        {
            return factory.NewNode<TShadow>(category: category, copyOnWrite: true /* In order to detect changes */);
        }

        static CustomNodeDesc<TShadow> AddDefaultPins<TShadow>(this CustomNodeDesc<TShadow> node)
            where TShadow : LightShadowMap, new()
        {
            return node
                .AddCachedInput(nameof(LightShadowMap.Filter), x => x.Filter, (x, v) => x.Filter = v)
                // Larger sizes can crash Stride when multiple lights / light shafts are used - one can find bunch of TODOs in ShadowMapRenderer regarding texture atlas limits
                .AddCachedInput(nameof(LightShadowMap.Size), x => x.Size, (x, v) => x.Size = v, LightShadowMapSize.Medium)
                .AddCachedInput(nameof(LightShadowMap.BiasParameters.DepthBias), x => x.BiasParameters.DepthBias, (x, v) => x.BiasParameters.DepthBias = v, 0.01f)
                .AddCachedInput(nameof(LightShadowMap.BiasParameters.NormalOffsetScale), x => x.BiasParameters.NormalOffsetScale, (x, v) => x.BiasParameters.NormalOffsetScale = v, 10f)
                .AddCachedInput(nameof(LightShadowMap.Debug), x => x.Debug, (x, v) => x.Debug = v);
        }

        static CustomNodeDesc<TShadow> AddEnabledPin<TShadow>(this CustomNodeDesc<TShadow> node)
            where TShadow : LightShadowMap, new()
        {
            return node.AddCachedInput(nameof(LightShadowMap.Enabled), x => x.Enabled, (x, v) => x.Enabled = v, true);
        }

        static IVLNodeDescription NewSkyboxNode(IVLNodeDescriptionFactory factory, string category)
        {
            return factory.NewNode(
                ctor: c => new SkyboxRenderer(c),
                name: "Skybox",
                category: category,
                copyOnWrite: false,
                hasStateOutput: false)
                .AddInput(nameof(SkyboxRenderer.CubeMap), x => x.CubeMap, (x, v) => x.CubeMap = v)
                .AddInput(nameof(SkyboxRenderer.IsSpecularOnly), x => x.IsSpecularOnly, (x, v) => x.IsSpecularOnly = v)
                .AddInput(nameof(SkyboxRenderer.DiffuseSHOrder), x => x.DiffuseSHOrder, (x, v) => x.DiffuseSHOrder = v, SkyboxPreFilteringDiffuseOrder.Order3)
                .AddInput(nameof(SkyboxRenderer.SpecularCubeMapSize), x => x.SpecularCubeMapSize, (x, v) => x.SpecularCubeMapSize = v, 256)
                .AddInput(nameof(SkyboxRenderer.ForceBuild), x => x.ForceBuild, (x, v) => x.ForceBuild = v)
                .AddOutput("Output", x =>
                {
                    x.ScheduleForRendering();
                    return x.Skybox;
                });
        }
    }
}

using Stride.Core.Mathematics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;
using Stride.Rendering.ProceduralModels;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Disposables;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.Mathematics;
using VL.Stride.Rendering.ComputeEffect;

namespace VL.Stride.Rendering
{
    using Model = global::Stride.Rendering.Model;

    static class RenderingNodes
    {
        public static IEnumerable<IVLNodeDescription> GetNodeDescriptions(IVLNodeDescriptionFactory factory)
        {
            var renderingCategory = "Stride.Rendering";
            var renderingAdvancedCategory = $"{renderingCategory}.Advanced";

            yield return NewInputRenderBaseNode<WithRenderTargetAndViewPort>(factory, category: renderingCategory)
                .AddInput(nameof(WithRenderTargetAndViewPort.RenderTarget), x => x.RenderTarget, (x, v) => x.RenderTarget = v)
                .AddInput(nameof(WithRenderTargetAndViewPort.DepthBuffer), x => x.DepthBuffer, (x, v) => x.DepthBuffer = v)
                ;

            yield return NewInputRenderBaseNode<RenderContextModifierRenderer>(factory, category: renderingCategory)
                .AddInput(nameof(RenderContextModifierRenderer.Modifier), x => x.Modifier, (x, v) => x.Modifier = v)
                ;

            yield return factory.NewNode<ParentTransformationModifier>(category: renderingCategory, copyOnWrite: false)
               .AddInput(nameof(ParentTransformationModifier.Transformation), x => x.Transformation, (x, v) => x.Transformation = v)
               .AddInput(nameof(ParentTransformationModifier.ExistingTransformUsage), x => x.ExistingTransformUsage, (x, v) => x.ExistingTransformUsage = v)
               ;

            yield return NewInputRenderBaseNode<WithinCommonSpace>(factory, category: renderingAdvancedCategory)
                .AddInput(nameof(WithinCommonSpace.CommonScreenSpace), x => x.CommonScreenSpace, (x, v) => x.CommonScreenSpace = v, CommonSpace.DIPTopLeft)
                ;

            yield return NewInputRenderBaseNode<WithinPhysicalScreenSpace>(factory, category: renderingAdvancedCategory)
                .AddInput(nameof(WithinPhysicalScreenSpace.Units), x => x.Units, (x, v) => x.Units = v, ScreenSpaceUnits.DIP)
                .AddInput(nameof(WithinPhysicalScreenSpace.Anchor), x => x.Anchor, (x, v) => x.Anchor = v, Lib.Mathematics.RectangleAnchor.Center)
                .AddInput(nameof(WithinPhysicalScreenSpace.Offset), x => x.Offset, (x, v) => x.Offset = v)
                .AddInput(nameof(WithinPhysicalScreenSpace.Scale), x => x.Scale, (x, v) => x.Scale = v, 1f)
                .AddInput(nameof(WithinPhysicalScreenSpace.IgnoreExistingView), x => x.IgnoreExistingView, (x, v) => x.IgnoreExistingView = v, true)
                .AddInput(nameof(WithinPhysicalScreenSpace.IgnoreExistingProjection), x => x.IgnoreExistingProjection, (x, v) => x.IgnoreExistingProjection = v, true)
                ;


            yield return NewInputRenderBaseNode<WithinVirtualScreenSpace>(factory, category: renderingAdvancedCategory)
                .AddInput(nameof(WithinVirtualScreenSpace.Bounds), x => x.Bounds, (x, v) => x.Bounds = v, new RectangleF(-0.5f, -0.5f, 1, 1))
                .AddInput(nameof(WithinVirtualScreenSpace.AspectRatioCorrectionMode), x => x.AspectRatioCorrectionMode, (x, v) => x.AspectRatioCorrectionMode = v, SizeMode.FitOut)
                .AddInput(nameof(WithinVirtualScreenSpace.Anchor), x => x.Anchor, (x, v) => x.Anchor = v, Lib.Mathematics.RectangleAnchor.Center)
                .AddInput(nameof(WithinVirtualScreenSpace.IgnoreExistingView), x => x.IgnoreExistingView, (x, v) => x.IgnoreExistingView = v, true)
                .AddInput(nameof(WithinVirtualScreenSpace.IgnoreExistingProjection), x => x.IgnoreExistingProjection, (x, v) => x.IgnoreExistingProjection = v, true)
                ;

            yield return NewInputRenderBaseNode<WithRenderView>(factory, category: renderingAdvancedCategory)
                .AddInput(nameof(WithRenderView.RenderView), x => x.RenderView, (x, v) => x.RenderView = v)
                .AddInput(nameof(WithRenderView.AspectRatioCorrectionMode), x => x.AspectRatioCorrectionMode, (x, v) => x.AspectRatioCorrectionMode = v)
                ;

            yield return NewInputRenderBaseNode<WithWindowInputSource>(factory, category: renderingAdvancedCategory)
                .AddInput(nameof(WithWindowInputSource.InputSource), x => x.InputSource, (x, v) => x.InputSource = v)
                ;

            yield return factory.NewNode<GetWindowInputSource>(name: nameof(GetWindowInputSource), category: renderingAdvancedCategory, copyOnWrite: false)
                .AddInput(nameof(RendererBase.Input), x => x.Input, (x, v) => x.Input = v)
                .AddOutput(nameof(GetWindowInputSource.InputSource), x => x.InputSource)
            ;

            // Compute effect dispatchers
            var dispatchersCategory = $"{renderingAdvancedCategory}.ComputeEffect";
            yield return factory.NewNode<DirectComputeEffectDispatcher>(name: "DirectDispatcher", category: renderingAdvancedCategory, copyOnWrite: false, hasStateOutput: false)
                .AddCachedInput(nameof(DirectComputeEffectDispatcher.ThreadGroupCount), x => x.ThreadGroupCount, (x, v) => x.ThreadGroupCount = v, Int3.One)
                .AddOutput<IComputeEffectDispatcher>("Output", x => x);

            yield return factory.NewNode<IndirectComputeEffectDispatcher>(name: "IndirectDispatcher", category: renderingAdvancedCategory, copyOnWrite: false, hasStateOutput: false)
                .AddCachedInput(nameof(IndirectComputeEffectDispatcher.ArgumentBuffer), x => x.ArgumentBuffer, (x, v) => x.ArgumentBuffer = v)
                .AddCachedInput(nameof(IndirectComputeEffectDispatcher.OffsetInBytes), x => x.OffsetInBytes, (x, v) => x.OffsetInBytes = v)
                .AddOutput<IComputeEffectDispatcher>("Output", x => x);

            yield return factory.NewNode<CustomComputeEffectDispatcher>(name: "CustomDispatcher", category: renderingAdvancedCategory, copyOnWrite: false, hasStateOutput: false)
                .AddCachedInput(nameof(CustomComputeEffectDispatcher.ThreadGroupCountsSelector), x => x.ThreadGroupCountsSelector, (x, v) => x.ThreadGroupCountsSelector = v)
                .AddOutput<IComputeEffectDispatcher>("Output", x => x);

            //yield return factory.NewNode<RenderView>(name: "RenderView", category: renderingAdvancedCategory, copyOnWrite: false)
            //    .AddInput(nameof(RenderView.View), x => x.View, (x, v) => x.View = v)
            //    .AddInput(nameof(RenderView.Projection), x => x.Projection, (x, v) => x.Projection = v)
            //    .AddInput(nameof(RenderView.NearClipPlane), x => x.NearClipPlane, (x, v) => x.NearClipPlane = v)
            //    .AddInput(nameof(RenderView.FarClipPlane), x => x.FarClipPlane, (x, v) => x.FarClipPlane = v)
            //    // TODO: add more
            //    ;

            // Meshes
            yield return factory.NewMeshNode((CapsuleProceduralModel x) => (x.Length, x.Radius, x.Tessellation))
                .AddCachedInput(nameof(CapsuleProceduralModel.Length), x => x.Length, (x, v) => x.Length = v, 0.5f)
                .AddCachedInput(nameof(CapsuleProceduralModel.Radius), x => x.Radius, (x, v) => x.Radius = v, 0.5f)
                .AddCachedInput(nameof(CapsuleProceduralModel.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v, 16)
                .AddDefaultPins();

            /*yield return factory.NewMeshNode((ConeProceduralModel x) => (x.Height, x.Radius, x.Tessellation))
                .AddCachedInput(nameof(ConeProceduralModel.Height), x => x.Height, (x, v) => x.Height = v, 1f)
                .AddCachedInput(nameof(ConeProceduralModel.Radius), x => x.Radius, (x, v) => x.Radius = v, 0.5f)
                .AddCachedInput(nameof(ConeProceduralModel.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v, 16)
                .AddDefaultPins();
            */

            /*yield return factory.NewMeshNode((CubeProceduralModel x) => x.Size, name: "BoxMesh")
                .AddCachedInput(nameof(CubeProceduralModel.Size), x => x.Size, (x, v) => x.Size = v, Vector3.One)
                .AddDefaultPins();
            */
            
            /*yield return factory.NewMeshNode((CylinderProceduralModel x) => (x.Height, x.Radius, x.Tessellation))
                .AddCachedInput(nameof(CylinderProceduralModel.Height), x => x.Height, (x, v) => x.Height = v, 1f)
                .AddCachedInput(nameof(CylinderProceduralModel.Radius), x => x.Radius, (x, v) => x.Radius = v, 0.5f)
                .AddCachedInput(nameof(CylinderProceduralModel.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v, 16)
                .AddDefaultPins();
            */

            yield return factory.NewMeshNode((GeoSphereProceduralModel x) => (x.Radius, x.Tessellation))
                .AddCachedInput(nameof(GeoSphereProceduralModel.Radius), x => x.Radius, (x, v) => x.Radius = v, 0.5f)
                .AddCachedInput(nameof(GeoSphereProceduralModel.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = VLMath.Clamp(v, 1, 5), 3)
                .AddDefaultPins();

            yield return factory.NewMeshNode((PlaneProceduralModel x) => (x.Size, x.Tessellation, x.Normal, x.GenerateBackFace))
                .AddCachedInput(nameof(PlaneProceduralModel.Size), x => x.Size, (x, v) => x.Size = v, Vector2.One)
                .AddCachedInput(nameof(PlaneProceduralModel.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v, Int2.One)
                .AddCachedInput(nameof(PlaneProceduralModel.Normal), x => x.Normal, (x, v) => x.Normal = v, NormalDirection.UpZ)
                .AddCachedInput(nameof(PlaneProceduralModel.GenerateBackFace), x => x.GenerateBackFace, (x, v) => x.GenerateBackFace = v, true)
                .AddDefaultPins();

            yield return factory.NewMeshNode((SphereProceduralModel x) => (x.Radius, x.Tessellation))
                .AddCachedInput(nameof(SphereProceduralModel.Radius), x => x.Radius, (x, v) => x.Radius = v, 0.5f)
                .AddCachedInput(nameof(SphereProceduralModel.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v, 16)
                .AddDefaultPins();

            yield return factory.NewMeshNode((TeapotProceduralModel x) => (x.Size, x.Tessellation))
                .AddCachedInput(nameof(TeapotProceduralModel.Size), x => x.Size, (x, v) => x.Size = v, 1f)
                .AddCachedInput(nameof(TeapotProceduralModel.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v, 16)
                .AddDefaultPins();

            yield return factory.NewMeshNode((TorusProceduralModel x) => (x.Radius, x.Thickness, x.Tessellation), tags: ImmutableArray.Create("donut"))
                .AddCachedInput(nameof(TorusProceduralModel.Radius), x => x.Radius, (x, v) => x.Radius = v, 0.5f)
                .AddCachedInput(nameof(TorusProceduralModel.Thickness), x => x.Thickness, (x, v) => x.Thickness = v, 0.25f)
                .AddCachedInput(nameof(TorusProceduralModel.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v, 16)
                .AddDefaultPins();

            #region g3 Primitives
            /* Leaving out for first iteration of this PR
            yield return factory.NewMeshNode((Models.ArrowMesh x) => (x.StickLength, x.StickRadius, x.HeadLength, x.HeadRadius, x.TipRadius, x.Tessellation, x.Anchor, x.SharedVertices, x.Clockwise), category: "Stride.Models.Meshes.Experimental")
                .AddCachedInput(nameof(Models.ArrowMesh.StickLength), x => x.StickLength, (x, v) => x.StickLength = v, 1f)
                .AddCachedInput(nameof(Models.ArrowMesh.StickRadius), x => x.StickRadius, (x, v) => x.StickRadius = v, 0.125f)
                .AddCachedInput(nameof(Models.ArrowMesh.HeadLength), x => x.HeadLength, (x, v) => x.HeadLength = v, 0.5f)
                .AddCachedInput(nameof(Models.ArrowMesh.HeadRadius), x => x.HeadRadius, (x, v) => x.HeadRadius = v, 0.3333f)
                .AddCachedInput(nameof(Models.ArrowMesh.TipRadius), x => x.TipRadius, (x, v) => x.TipRadius = v, 0f)
                .AddCachedInput(nameof(Models.ArrowMesh.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v, 16)
                .AddCachedInput(nameof(Models.ArrowMesh.Anchor), x => x.Anchor, (x, v) => x.Anchor = v, Models.AnchorMode.Middle)
                .AddCachedInput(nameof(Models.ArrowMesh.SharedVertices), x => x.SharedVertices, (x, v) => x.SharedVertices = v, false)
                .AddCachedInput(nameof(Models.ArrowMesh.Clockwise), x => x.Clockwise, (x, v) => x.Clockwise = v, false, null, null, false)
                .AddDefaultPins();
            */

            yield return factory.NewMeshNode((Models.BoxMesh x) => (x.Size, x.Tessellation, x.Anchor, x.Clockwise))
                .AddCachedInput(nameof(Models.BoxMesh.Size), x => x.Size, (x, v) => x.Size = v, Vector3.One)
                .AddCachedInput(nameof(Models.BoxMesh.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v, 1)
                .AddCachedInput(nameof(Models.BoxMesh.Anchor), x => x.Anchor, (x, v) => x.Anchor = v, Models.AnchorMode.Middle)
                .AddCachedInput(nameof(Models.BoxMesh.Clockwise), x => x.Clockwise, (x, v) => x.Clockwise = v, false, null, null, false)
                .AddDefaultPins();
            
            yield return factory.NewMeshNode((Models.BoxSphereMesh x) => (x.Radius, x.Tessellation, x.Anchor, x.Clockwise))
                .AddCachedInput(nameof(Models.BoxSphereMesh.Radius), x => x.Radius, (x, v) => x.Radius = v, 0.5f)
                .AddCachedInput(nameof(Models.BoxSphereMesh.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v, 8)
                .AddCachedInput(nameof(Models.BoxSphereMesh.Anchor), x => x.Anchor, (x, v) => x.Anchor = v, Models.AnchorMode.Middle)
                .AddCachedInput(nameof(Models.BoxSphereMesh.Clockwise), x => x.Clockwise, (x, v) => x.Clockwise = v, false, null, null, false)
                .AddDefaultPins();

            yield return factory.NewMeshNode((Models.ConeMesh x) => (x.Height, x.Radius, x.FromAngle, x.ToAngle, x.Capped, x.GenerateBackFace, x.Tessellation, x.Anchor, x.SlopeUVMode, x.Clockwise))
                .AddCachedInput(nameof(Models.ConeMesh.Height), x => x.Height, (x, v) => x.Height = v, 1f)
                .AddCachedInput(nameof(Models.ConeMesh.Radius), x => x.Radius, (x, v) => x.Radius = v, 0.5f)
                .AddCachedInput(nameof(Models.ConeMesh.FromAngle), x => x.FromAngle, (x, v) => x.FromAngle = v, 0f)
                .AddCachedInput(nameof(Models.ConeMesh.ToAngle), x => x.ToAngle, (x, v) => x.ToAngle = v, 1f)
                .AddCachedInput(nameof(Models.ConeMesh.Capped), x => x.Capped, (x, v) => x.Capped = v, true)
                .AddCachedInput(nameof(Models.ConeMesh.GenerateBackFace), x => x.GenerateBackFace, (x, v) => x.GenerateBackFace = v, false)
                .AddCachedInput(nameof(Models.ConeMesh.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v, new Int2(16, 1))
                .AddCachedInput(nameof(Models.ConeMesh.Anchor), x => x.Anchor, (x, v) => x.Anchor = v, Models.AnchorMode.Middle)
                .AddCachedInput(nameof(Models.ConeMesh.SlopeUVMode), x => x.SlopeUVMode, (x, v) => x.SlopeUVMode = v, Models.SlopeUVMode.SideProjected)
                .AddCachedInput(nameof(Models.ConeMesh.Clockwise), x => x.Clockwise, (x, v) => x.Clockwise = v, false, null, null, false)
                .AddDefaultPins();

            yield return factory.NewMeshNode((Models.CylinderMesh x) => (x.Height, x.BaseRadius, x.TopRadius, x.FromAngle, x.ToAngle, x.Capped, x.GenerateBackFace, x.Tessellation, x.Anchor, x.Clockwise))
                .AddCachedInput(nameof(Models.CylinderMesh.Height), x => x.Height, (x, v) => x.Height = v, 1f)
                .AddCachedInput(nameof(Models.CylinderMesh.BaseRadius), x => x.BaseRadius, (x, v) => x.BaseRadius = v, 0.5f)
                .AddCachedInput(nameof(Models.CylinderMesh.TopRadius), x => x.TopRadius, (x, v) => x.TopRadius = v, 0.5f)
                .AddCachedInput(nameof(Models.CylinderMesh.FromAngle), x => x.FromAngle, (x, v) => x.FromAngle = v, 0f)
                .AddCachedInput(nameof(Models.CylinderMesh.ToAngle), x => x.ToAngle, (x, v) => x.ToAngle = v, 1f)
                .AddCachedInput(nameof(Models.CylinderMesh.Capped), x => x.Capped, (x, v) => x.Capped = v, true)
                .AddCachedInput(nameof(Models.CylinderMesh.GenerateBackFace), x => x.GenerateBackFace, (x, v) => x.GenerateBackFace = v, false)
                .AddCachedInput(nameof(Models.CylinderMesh.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v, new Int2(16, 1))
                .AddCachedInput(nameof(Models.CylinderMesh.Anchor), x => x.Anchor, (x, v) => x.Anchor = v, Models.AnchorMode.Middle)
                .AddCachedInput(nameof(Models.CylinderMesh.Clockwise), x => x.Clockwise, (x, v) => x.Clockwise = v, false, null, null, false)
                .AddDefaultPins();

            yield return factory.NewMeshNode((Models.DiscMesh x) => (x.OuterRadius, x.InnerRadius, x.FromAngle, x.ToAngle, x.Normal, x.GenerateBackFace, x.Tessellation, x.Clockwise), tags: ImmutableArray.Create("circle", "segment"))
                .AddCachedInput(nameof(Models.DiscMesh.OuterRadius), x => x.OuterRadius, (x, v) => x.OuterRadius = v, 0.5f)
                .AddCachedInput(nameof(Models.DiscMesh.InnerRadius), x => x.InnerRadius, (x, v) => x.InnerRadius = v, 0.25f)
                .AddCachedInput(nameof(Models.DiscMesh.FromAngle), x => x.FromAngle, (x, v) => x.FromAngle = v, 0f)
                .AddCachedInput(nameof(Models.DiscMesh.ToAngle), x => x.ToAngle, (x, v) => x.ToAngle = v, 1f)
                .AddCachedInput(nameof(Models.DiscMesh.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v, 16)
                .AddCachedInput(nameof(Models.DiscMesh.Normal), x => x.Normal, (x, v) => x.Normal = v, NormalDirection.UpZ)
                .AddCachedInput(nameof(Models.DiscMesh.GenerateBackFace), x => x.GenerateBackFace, (x, v) => x.GenerateBackFace = v, true)
                .AddCachedInput(nameof(Models.DiscMesh.Clockwise), x => x.Clockwise, (x, v) => x.Clockwise = v, false, null, null, false)
                .AddDefaultPins();

            yield return factory.NewMeshNode((Models.RoundRectangleMesh x) => (x.Size, x.Radius, x.SharpCorners, x.CornerTessellation, x.Normal, x.GenerateBackFace, x.Clockwise))
                .AddCachedInput(nameof(Models.RoundRectangleMesh.Size), x => x.Size, (x, v) => x.Size = v, Vector2.One)
                .AddCachedInput(nameof(Models.RoundRectangleMesh.Radius), x => x.Radius, (x, v) => x.Radius = v, 0.25f)
                .AddCachedInput(nameof(Models.RoundRectangleMesh.SharpCorners), x => x.SharpCorners, (x, v) => x.SharpCorners = v, Models.RoundRectangleMesh.SharpCorner.None)
                .AddCachedInput(nameof(Models.RoundRectangleMesh.CornerTessellation), x => x.CornerTessellation, (x, v) => x.CornerTessellation = v, 4)
                .AddCachedInput(nameof(Models.RoundRectangleMesh.Normal), x => x.Normal, (x, v) => x.Normal = v, NormalDirection.UpZ)
                .AddCachedInput(nameof(Models.RoundRectangleMesh.GenerateBackFace), x => x.GenerateBackFace, (x, v) => x.GenerateBackFace = v, true)
                .AddCachedInput(nameof(Models.RoundRectangleMesh.Clockwise), x => x.Clockwise, (x, v) => x.Clockwise = v, false, null, null, false)
                .AddDefaultPins();

            /* Leaving out for first iteration of this PR
            yield return factory.NewMeshNode((Models.TubeMesh x) => (x.Path, x.Closed, x.Shape, x.Capped, x.SharedVertices, x.Clockwise), category: "Stride.Models.Meshes.Experimental")
                .AddCachedInput(nameof(Models.TubeMesh.Path), x => x.Path, (x, v) => x.Path = v)
                .AddCachedInput(nameof(Models.TubeMesh.Closed), x => x.Closed, (x, v) => x.Closed = v,false)
                .AddCachedInput(nameof(Models.TubeMesh.Shape), x => x.Shape, (x, v) => x.Shape = v)
                .AddCachedInput(nameof(Models.TubeMesh.Capped), x => x.Capped, (x, v) => x.Capped = v, true)
                .AddCachedInput(nameof(Models.TubeMesh.SharedVertices), x => x.SharedVertices, (x, v) => x.SharedVertices = v, false)
                .AddCachedInput(nameof(Models.TubeMesh.Clockwise), x => x.Clockwise, (x, v) => x.Clockwise = v, false, null, null, false)
                .AddDefaultPins();
            
            yield return factory.NewMeshNode((Models.VerticalGeneralizedCylinderMesh x) => (x.Capped, x.Sections, x.Tessellation, x.Anchor, x.SharedVertices, x.Clockwise), category: "Stride.Models.Meshes.Experimental")
                .AddCachedInput(nameof(Models.VerticalGeneralizedCylinderMesh.Capped), x => x.Capped, (x, v) => x.Capped = v, true)
                .AddCachedInput(nameof(Models.VerticalGeneralizedCylinderMesh.Sections), x => x.Sections, (x, v) => x.Sections = v)
                .AddCachedInput(nameof(Models.VerticalGeneralizedCylinderMesh.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v, 16)
                .AddCachedInput(nameof(Models.VerticalGeneralizedCylinderMesh.Anchor), x => x.Anchor, (x, v) => x.Anchor = v, Models.AnchorMode.Middle)
                .AddCachedInput(nameof(Models.VerticalGeneralizedCylinderMesh.SharedVertices), x => x.SharedVertices, (x, v) => x.SharedVertices = v, false)
                .AddCachedInput(nameof(Models.VerticalGeneralizedCylinderMesh.Clockwise), x => x.Clockwise, (x, v) => x.Clockwise = v, false, null, null, false)
                .AddDefaultPins();
            */
            #endregion g3 Primitives

            // TextureFX
            yield return factory.NewNode(c => new MipMapGenerator(c), name: "MipMap", category: "Stride.Textures.Experimental.Utils", copyOnWrite: false, hasStateOutput: false)
                .AddInput("Input", x => x.InputTexture, (x, v) => x.InputTexture = v)
                .AddInput(nameof(MipMapGenerator.MaxMipMapCount), x => x.MaxMipMapCount, (x, v) => x.MaxMipMapCount = v)
                .AddOutput("Output", x => { x.ScheduleForRendering(); return x.OutputTexture; });
        }

        static CustomNodeDesc<TInputRenderBase> NewInputRenderBaseNode<TInputRenderBase>(IVLNodeDescriptionFactory factory, string category, string name = null)
            where TInputRenderBase : RendererBase, new()
        {
            return factory.NewNode<TInputRenderBase>(name: name, category: category, copyOnWrite: false)
                .AddInput(nameof(RendererBase.Input), x => x.Input, (x, v) => x.Input = v);
        }

        static CustomNodeDesc<TProceduralModel> NewMeshNode<TProceduralModel, TKey>(this IVLNodeDescriptionFactory factory, Func<TProceduralModel, TKey> getKey, string category = "Stride.Models.Meshes", string name = null, ImmutableArray<string> tags = default)
           where TProceduralModel : PrimitiveProceduralModelBase, new()
        {
            return new CustomNodeDesc<TProceduralModel>(factory,
                name: name ?? typeof(TProceduralModel).Name.Replace("ProceduralModel", "Mesh"),
                category: category,
                copyOnWrite: false,
                hasStateOutput: false,
                ctor: nodeContext =>
                {
                    var generator = new TProceduralModel();
                    return (generator, default);
                }, 
                tags:tags)
                .AddCachedOutput<Mesh>("Output", lifetime =>
                {
                    var disposable = new SerialDisposable();
                    lifetime.Add(disposable);

                    var gameProvider = IAppHost.Current.Services.GetGameProvider();
                    return generator =>
                    {
                        var key = (gameProvider, typeof(TProceduralModel), generator.Scale, generator.UvScale, generator.LocalOffset, generator.NumberOfTextureCoordinates, getKey(generator));
                        var provider = ResourceProvider.NewPooledSystemWide(key, _ =>
                        {
                            return gameProvider.Bind(
                                game =>
                                {
                                    var model = new Model();
                                    generator.Generate(game.Services, model);
                                    return ResourceProvider.Return(model.Meshes[0], m =>
                                    {
                                        if (m.Draw != null)
                                        {
                                            m.Draw.IndexBuffer?.Buffer?.Dispose();
                                            foreach (var b in m.Draw.VertexBuffers)
                                                b.Buffer?.Dispose();
                                        }
                                    });
                                });
                        });
                        var meshHandle = provider.GetHandle();
                        disposable.Disposable = meshHandle;
                        return meshHandle.Resource;
                    };
                });
        }

        static CustomNodeDesc<TProceduralModel> AddDefaultPins<TProceduralModel>(this CustomNodeDesc<TProceduralModel> node)
            where TProceduralModel : PrimitiveProceduralModelBase, new()
        {
            return node
                .AddCachedInput(nameof(PrimitiveProceduralModelBase.Scale), x => x.Scale, (x, v) => x.Scale = v, Vector3.One)
                .AddCachedInput(nameof(PrimitiveProceduralModelBase.UvScale), x => x.UvScale, (x, v) => x.UvScale = v, Vector2.One, isVisible: false)
                .AddCachedInput(nameof(PrimitiveProceduralModelBase.LocalOffset), x => x.LocalOffset, (x, v) => x.LocalOffset = v, Vector3.Zero)
                .AddCachedInput(nameof(PrimitiveProceduralModelBase.NumberOfTextureCoordinates), x => x.NumberOfTextureCoordinates, (x, v) => x.NumberOfTextureCoordinates = v, 1);
        }
    }
}

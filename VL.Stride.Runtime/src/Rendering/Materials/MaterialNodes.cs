using Silk.NET.SDL;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;

namespace VL.Stride.Rendering.Materials
{
    static class MaterialNodes
    {
        public static IEnumerable<IVLNodeDescription> GetNodeDescriptions(IVLNodeDescriptionFactory nodeFactory)
        {
            string materialCategory = "Stride.Materials";
            string materialAdvancedCategory = "Stride.Advanced.Materials";
            string geometryCategory = $"{materialCategory}.{nameof(GeometryAttributes)}";
            string geometryAdvancedCategory = $"{materialAdvancedCategory}.{nameof(GeometryAttributes)}";
            string shadingCategory = $"{materialCategory}.{nameof(ShadingAttributes)}";
            string shadingAdvancedCategory = $"{materialAdvancedCategory}.{nameof(ShadingAttributes)}";
            string miscCategory = $"{materialCategory}.{nameof(MiscAttributes)}";
            string miscAdvancedCategory = $"{materialAdvancedCategory}.{nameof(MiscAttributes)}";
            string transparencyCategory = $"{miscCategory}.Transparency";
            string transparencyAdvancedCategory = $"{miscAdvancedCategory}.Transparency";
            string layersCategory = $"{materialCategory}.Layers";
            string layersAdvancedCategory = $"{materialAdvancedCategory}.Layers";
            string diffuseModelAdvancedCategory = $"{shadingAdvancedCategory}.DiffuseModel";
            string specularModelAdvancedCategory = $"{shadingAdvancedCategory}.SpecularModel";
            string subsurfaceScatteringCategory = $"{shadingCategory}.SubsurfaceScattering";
            string subsurfaceScatteringAdvancedCategory = $"{shadingAdvancedCategory}.SubsurfaceScattering";

            // Geometry
            yield return nodeFactory.NewNode<GeometryAttributes>(category: materialAdvancedCategory)
                .AddCachedInput(nameof(GeometryAttributes.Tessellation), x => x.Tessellation, (x, v) => x.Tessellation = v)
                .AddCachedInput(nameof(GeometryAttributes.Displacement), x => x.Displacement, (x, v) => x.Displacement = v)
                .AddCachedInput(nameof(GeometryAttributes.Surface), x => x.Surface, (x, v) => x.Surface = v)
                .AddCachedInput(nameof(GeometryAttributes.MicroSurface), x => x.MicroSurface, (x, v) => x.MicroSurface = v);

            yield return NewMaterialNode<MaterialTessellationFlatFeature>(nodeFactory, "FlatTessellation", geometryCategory);
            yield return NewMaterialNode<MaterialTessellationPNFeature>(nodeFactory, "PointNormalTessellation", geometryCategory);
            yield return NewMaterialNode<MaterialDisplacementMapFeature>(nodeFactory, "Displacement", geometryCategory);
            yield return NewMaterialNode<MaterialNormalMapFeature>(nodeFactory, "Normal", geometryCategory);
            yield return NewMaterialNode<MaterialGlossinessMapFeature>(nodeFactory, "Glossiness", geometryAdvancedCategory);

            // Shading
            yield return nodeFactory.NewNode<ShadingAttributes>(category: materialAdvancedCategory)
                .AddCachedInput(nameof(ShadingAttributes.Diffuse), x => x.Diffuse, (x, v) => x.Diffuse = v)
                .AddCachedInput(nameof(ShadingAttributes.DiffuseModel), x => x.DiffuseModel, (x, v) => x.DiffuseModel = v)
                .AddCachedInput(nameof(ShadingAttributes.Specular), x => x.Specular, (x, v) => x.Specular = v)
                .AddCachedInput(nameof(ShadingAttributes.SpecularModel), x => x.SpecularModel, (x, v) => x.SpecularModel = v)
                .AddCachedInput(nameof(ShadingAttributes.Emissive), x => x.Emissive, (x, v) => x.Emissive = v)
                .AddCachedInput(nameof(ShadingAttributes.SubsurfaceScattering), x => x.SubsurfaceScattering, (x, v) => x.SubsurfaceScattering = v);

            yield return NewMaterialNode<MaterialDiffuseMapFeature>(nodeFactory, "Diffuse", shadingAdvancedCategory);

            yield return NewMaterialNode<MaterialDiffuseCelShadingModelFeature>(nodeFactory, "CelShading", diffuseModelAdvancedCategory);
            yield return NewMaterialNode<MaterialDiffuseHairModelFeature>(nodeFactory, "Hair", diffuseModelAdvancedCategory);
            yield return NewMaterialNode<MaterialDiffuseLambertModelFeature>(nodeFactory, "Lambert", diffuseModelAdvancedCategory);

            yield return NewMaterialNode<MaterialMetalnessMapFeature>(nodeFactory, "Metalness", shadingAdvancedCategory);
            yield return NewMaterialNode<MaterialSpecularMapFeature>(nodeFactory, "Specular", shadingAdvancedCategory);

            yield return nodeFactory.NewNode<MaterialSpecularCelShadingModelFeature>("CelShading", specularModelAdvancedCategory)
                .AddInputs()
                .AddCachedInput(nameof(MaterialSpecularCelShadingModelFeature.RampFunction), x => x.RampFunction, (x, v) => x.RampFunction = v);

            yield return NewMaterialNode<MaterialCelShadingLightDefault>(nodeFactory, "DefaultLightFunction", $"{specularModelAdvancedCategory}.CelShading");
            yield return NewMaterialNode<MaterialCelShadingLightRamp>(nodeFactory, "RampLightFunction", $"{specularModelAdvancedCategory}.CelShading");

            yield return NewMaterialNode<MaterialSpecularHairModelFeature>(nodeFactory, "Hair", specularModelAdvancedCategory);

            yield return nodeFactory.NewNode<MaterialSpecularMicrofacetModelFeature>("Microfacet", specularModelAdvancedCategory)
                .AddInputs();

            var defaultGlass = new MaterialSpecularThinGlassModelFeature();
            yield return nodeFactory.NewNode<MaterialSpecularThinGlassModelFeature>("Glass", specularModelAdvancedCategory)
                .AddInputs()
                .AddCachedInput(nameof(MaterialSpecularThinGlassModelFeature.RefractiveIndex), x => x.RefractiveIndex, (x, v) => x.RefractiveIndex = v, defaultGlass.RefractiveIndex);

            yield return NewMaterialNode<MaterialEmissiveMapFeature>(nodeFactory, "Emissive", shadingCategory);
            yield return NewMaterialNode<MaterialSubsurfaceScatteringFeature>(nodeFactory, "SubsurfaceScattering", shadingCategory);
            yield return new StrideNodeDesc<MaterialSubsurfaceScatteringScatteringKernelSkin>(nodeFactory, "SkinKernel", subsurfaceScatteringCategory);
            yield return new StrideNodeDesc<MaterialSubsurfaceScatteringScatteringProfileSkin>(nodeFactory, "SkinProfile", subsurfaceScatteringCategory);
            yield return new StrideNodeDesc<MaterialSubsurfaceScatteringScatteringProfileCustom>(nodeFactory, "CustomProfile", subsurfaceScatteringCategory);
            yield return new StrideNodeDesc<FallbackMaterial>(nodeFactory, category: materialAdvancedCategory);

            // Misc
            yield return nodeFactory.NewNode<MiscAttributes>(category: materialAdvancedCategory)
                .AddCachedInput(nameof(MiscAttributes.Occlusion), x => x.Occlusion, (x, v) => x.Occlusion = v)
                .AddCachedInput(nameof(MiscAttributes.Transparency), x => x.Transparency, (x, v) => x.Transparency = v)
                .AddCachedInput(nameof(MiscAttributes.Overrides), x => x.Overrides, (x, v) => x.Overrides = v)
                .AddCachedInput(nameof(MiscAttributes.CullMode), x => x.CullMode, (x, v) => x.CullMode = v, CullMode.Back)
                .AddCachedInput(nameof(MiscAttributes.ClearCoat), x => x.ClearCoat, (x, v) => x.ClearCoat = v);
            yield return NewMaterialNode<MaterialOcclusionMapFeature>(nodeFactory, "Occlusion", miscCategory);
            yield return NewMaterialNode<MaterialTransparencyAdditiveFeature>(nodeFactory, "Additive", transparencyCategory);
            yield return NewMaterialNode<MaterialTransparencyBlendFeature>(nodeFactory, "Blend", transparencyCategory);
            yield return NewMaterialNode<MaterialTransparencyCutoffFeature>(nodeFactory, "Cutoff", transparencyCategory);
            yield return NewMaterialNode<MaterialClearCoatFeature>(nodeFactory, "ClearCoat", miscAdvancedCategory);

            // Layers
            yield return NewMaterialNode<MaterialBlendLayer>(nodeFactory, "MaterialLayer", layersCategory);
            yield return NewMaterialNode<MaterialOverrides>(nodeFactory, "LayerOverrides", layersCategory);

            // Top level
            yield return nodeFactory.NewNode(
                name: "Material", 
                category: materialAdvancedCategory,
                ctor: ctx => new MaterialBuilder(ctx),
                copyOnWrite: false,
                hasStateOutput: false)
                .AddCachedInput(nameof(MaterialBuilder.Geometry), x => x.Geometry, (x, v) => x.Geometry = v)
                .AddCachedInput(nameof(MaterialBuilder.Shading), x => x.Shading, (x, v) => x.Shading = v)
                .AddCachedInput(nameof(MaterialBuilder.Misc), x => x.Misc, (x, v) => x.Misc = v)
                .AddCachedListInput(nameof(MaterialBuilder.Layers), x => x.Layers)
                .AddCachedOutput("Output", x => x.ToMaterial());

            yield return nodeFactory.NewNode(
                name: "Material (Descriptor Internal)",
                category: materialAdvancedCategory,
                ctor: ctx => new MaterialBuilderFromDescriptor(ctx),
                copyOnWrite: false,
                hasStateOutput: false)
                .AddCachedInput(nameof(MaterialBuilderFromDescriptor.Descriptor), x => x.Descriptor, (x, v) => x.Descriptor = v)
                .AddCachedOutput("Output", x => x.ToMaterial());

            yield return nodeFactory.NewNode(
                name: "MaterialExtension",
                category: materialAdvancedCategory,
                ctor: ctx => new MaterialBuilderFromMaterial(ctx),
                copyOnWrite: false,
                hasStateOutput: false)
                .AddOptimizedInput(nameof(MaterialBuilderFromMaterial.Material), x => x.Material, (x, v) => x.Material = v)
                .AddOptimizedInput(nameof(MaterialBuilderFromMaterial.MaterialExtension), x => x.MaterialExtension, (x, v) => x.MaterialExtension = v)
                .AddOptimizedInput(nameof(MaterialBuilderFromMaterial.TessellationExtension), x => x.TessellationExtension, (x, v) => x.TessellationExtension = v, summary: "Connected shader will be on Domain Stage, so you can use Tessellation (HSMain, HSConstantMain and DSMain)")
                .AddOptimizedInput(nameof(MaterialBuilderFromMaterial.TessellationStream), x => x.TessellationStream, (x, v) => x.TessellationStream = v, summary: "Name of the stream that will be reachable on Domain Stage")
                .AddOptimizedInput(nameof(MaterialBuilderFromMaterial.VertexAddition), x => x.VertexAddition, (x, v) => x.VertexAddition = v, isVisible: false, summary: "Connected shader must inherit from IMaterialSurface")
                .AddOptimizedInput(nameof(MaterialBuilderFromMaterial.PixelAddition), x => x.PixelAddition, (x, v) => x.PixelAddition = v, isVisible: false, summary: "Connected shader must inherit from IMaterialSurface")
                .AddOptimizedInput(nameof(MaterialBuilderFromMaterial.Cutoff), x => x.Cutoff, (x, v) => x.Cutoff = v, summary: "Sets the transparency feature of the material to Cutoff, needed when writing into depth in the pixel shader")
                .AddCachedOutput("Output", x => x.ToMaterial());

            yield return nodeFactory.NewNode(
                name: "CustomTessellation",
                category: materialAdvancedCategory,
                ctor: ctx => new MaterialCustomTessellationFeature(),
                copyOnWrite: false,
                hasStateOutput: false)
                .AddOptimizedInput(nameof(MaterialCustomTessellationFeature.TessellationShader), x => x.TessellationShader, (x, v) => x.TessellationShader = v)
                .AddCachedInput(nameof(MaterialCustomTessellationFeature.TessellationStream), x => x.TessellationStream, (x, v) => x.TessellationStream = v)
                .AddCachedOutput("Output", x => x);
        }

        static StrideNodeDesc<T> NewMaterialNode<T>(this IVLNodeDescriptionFactory nodeFactory, string name, string category)
            where T : new()
        {
            return new StrideNodeDesc<T>(nodeFactory, name, category);
        }

        static CustomNodeDesc<T> AddInputs<T>(this CustomNodeDesc<T> node)
            where T : MaterialSpecularMicrofacetModelFeature, new()
        {
            var i = new T();
            return node
                .AddCachedInput(nameof(MaterialSpecularMicrofacetModelFeature.Fresnel), x => x.Fresnel.ToEnum(), (x, v) => x.Fresnel = v.ToFunction(), i.Fresnel.ToEnum())
                .AddCachedInput(nameof(MaterialSpecularMicrofacetModelFeature.Visibility), x => x.Visibility.ToEnum(), (x, v) => x.Visibility = v.ToFunction(), i.Visibility.ToEnum())
                .AddCachedInput(nameof(MaterialSpecularMicrofacetModelFeature.NormalDistribution), x => x.NormalDistribution.ToEnum(), (x, v) => x.NormalDistribution = v.ToFunction(), i.NormalDistribution.ToEnum())
                .AddCachedInput(nameof(MaterialSpecularMicrofacetModelFeature.Environment), x => x.Environment.ToEnum(), (x, v) => x.Environment = v.ToFunction(), i.Environment.ToEnum());
        }

        static CustomNodeDesc<T> AddStateOutputWithRefEquality<T>(this CustomNodeDesc<T> node)
            where T : MaterialSpecularMicrofacetModelFeature, new()
        {
            var i = new T();
            return node
                .AddOutput("Output", x => x)
                .AddCachedInput(nameof(MaterialSpecularMicrofacetModelFeature.Fresnel), x => x.Fresnel.ToEnum(), (x, v) => x.Fresnel = v.ToFunction(), i.Fresnel.ToEnum())
                .AddCachedInput(nameof(MaterialSpecularMicrofacetModelFeature.Visibility), x => x.Visibility.ToEnum(), (x, v) => x.Visibility = v.ToFunction(), i.Visibility.ToEnum())
                .AddCachedInput(nameof(MaterialSpecularMicrofacetModelFeature.NormalDistribution), x => x.NormalDistribution.ToEnum(), (x, v) => x.NormalDistribution = v.ToFunction(), i.NormalDistribution.ToEnum())
                .AddCachedInput(nameof(MaterialSpecularMicrofacetModelFeature.Environment), x => x.Environment.ToEnum(), (x, v) => x.Environment = v.ToFunction(), i.Environment.ToEnum());
        }
    }

    /// <summary>
    /// A material defines the appearance of a 3D model surface and how it reacts to light.
    /// </summary>
    internal class MaterialBuilder : IDisposable
    {
        readonly MaterialAttributes @default = new MaterialAttributes();
        readonly MaterialBuilderFromDescriptor builder;

        public MaterialBuilder(NodeContext nodeContext)
        {
            builder = new MaterialBuilderFromDescriptor(nodeContext);
        }

        public void Dispose()
        {
            builder.Dispose();
        }

        /// <summary>
        /// The shape of the material.
        /// </summary>
        public GeometryAttributes Geometry { get; set; }

        /// <summary>
        /// The color characteristics of the material and how it reacts to light.
        /// </summary>
        public ShadingAttributes Shading { get; set; }

        /// <summary>
        /// Occlusion, transparency and clear coat shading.
        /// </summary>
        public MiscAttributes Misc { get; set; }

        /// <summary>
        /// The material layers to build more complex materials.
        /// </summary>
        public MaterialBlendLayers Layers { get; set; } = new MaterialBlendLayers();

        public MaterialAttributes ToMaterialAttributes()
        {
            return new MaterialAttributes()
            {
                Tessellation = Geometry?.Tessellation,
                Displacement = Geometry?.Displacement,
                Surface = Geometry?.Surface,
                MicroSurface = Geometry?.MicroSurface,
                Diffuse = Shading?.Diffuse,
                DiffuseModel = Shading?.DiffuseModel,
                Specular = Shading?.Specular,
                SpecularModel = Shading?.SpecularModel,
                Emissive = Shading?.Emissive,
                SubsurfaceScattering = Shading?.SubsurfaceScattering,
                Occlusion = Misc?.Occlusion,
                Transparency = Misc?.Transparency,
                Overrides = { UVScale = Misc?.Overrides?.UVScale ?? @default.Overrides.UVScale },
                CullMode = Misc?.CullMode ?? @default.CullMode,
                ClearCoat = Misc?.ClearCoat
            };
        }

        public Material ToMaterial()
        {
            var descriptor = new MaterialDescriptor()
            {
                Attributes = ToMaterialAttributes(),
                Layers = Layers
            };
            builder.Descriptor = descriptor;
            return builder.ToMaterial();
        }
    }

    /// <summary>
    /// A material defines the appearance of a 3D model surface and how it reacts to light.
    /// </summary>
    internal class MaterialBuilderFromDescriptor : IDisposable
    {
        readonly IResourceHandle<Game> gameHandle;
        readonly SerialDisposable subscriptions = new SerialDisposable();

        public MaterialBuilderFromDescriptor(NodeContext nodeContext)
        {
            gameHandle = AppHost.Current.Services.GetGameHandle();
        }

        public void Dispose()
        {
            subscriptions.Dispose();
            gameHandle.Dispose();
        }

        /// <summary>
        /// The material descriptor.
        /// </summary>
        public MaterialDescriptor Descriptor { get; set; }

        public Material ToMaterial()
        {
            var game = gameHandle.Resource;
            var s = new CompositeDisposable();
            subscriptions.Disposable = s;
            return MaterialExtensions.New(game.GraphicsDevice, Descriptor ?? new MaterialDescriptor(), game.Content, s);
        }
    }

    /// <summary>
    /// A material defines the appearance of a 3D model surface and how it reacts to light.
    /// </summary>
    internal class MaterialBuilderFromMaterial : IDisposable
    {
        readonly MaterialBuilderFromDescriptor builder;
        readonly MaterialDescriptor defaultDescriptor;

        public MaterialBuilderFromMaterial(NodeContext nodeContext)
        {
            builder = new MaterialBuilderFromDescriptor(nodeContext);
            defaultDescriptor = new MaterialDescriptor()
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor()),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                },
            };
        }

        public void Dispose()
        {
            builder.Dispose();
        }

        readonly IMaterialTransparencyFeature transparencyFeature = new MaterialTransparencyCutoffFeature();
        readonly VLMaterialEmissiveFeature emissiveFeature = new VLMaterialEmissiveFeature();
        readonly VLMaterialTessellationFeature tesselationFeature = new VLMaterialTessellationFeature();

        /// <summary>
        /// The material descriptor.
        /// </summary>
        public Material Material { get; set; }
        public bool Cutoff { get; set; }
        public IComputeNode MaterialExtension { get; set; }
        public IComputeNode TessellationExtension { get; set; }
        public string TessellationStream { get; set; }
        public IComputeNode VertexAddition { get; set; }
        public IComputeNode PixelAddition { get; set; }

        public Material ToMaterial()
        {
            var descriptor = Material?.Descriptor ?? defaultDescriptor;
            if (descriptor != null)
            {
                var origEmissive = descriptor.Attributes.Emissive;
                var origTransparency = descriptor.Attributes.Transparency;
                var origTesselation = descriptor.Attributes.Tessellation;

                try
                {
                    emissiveFeature.MaterialEmissiveFeature = origEmissive;
                    emissiveFeature.MaterialExtension = MaterialExtension;
                    emissiveFeature.VertexAddition = VertexAddition;
                    emissiveFeature.PixelAddition = PixelAddition;

                    // set new attributes
                    descriptor.Attributes.Emissive = emissiveFeature;

                    if (Cutoff)
                        descriptor.Attributes.Transparency = transparencyFeature;

                    // Tesselation feature
                    tesselationFeature.MaterialTessellationFeature = origTesselation;
                    tesselationFeature.MaterialExtension = TessellationExtension;

                    // add tessellation streams
                    tesselationFeature.MaterialTessellationStream = TessellationStream;
                    // set new attributes
                    descriptor.Attributes.Tessellation = tesselationFeature;

                    builder.Descriptor = descriptor;

                    return builder.ToMaterial();
                }
                finally
                {
                    // reset attributes
                    descriptor.Attributes.Emissive = origEmissive;
                    descriptor.Attributes.Transparency = origTransparency;
                    descriptor.Attributes.Tessellation = origTesselation;
                }
            }

            return Material;
        }
    }

    /// <summary>
    /// The material geometry attributes define the shape of a material.
    /// </summary>
    public class GeometryAttributes
    {
        /// <summary>
        /// Gets or sets the tessellation.
        /// </summary>
        /// <value>The tessellation.</value>
        /// <userdoc>The method used for tessellation (subdividing model poligons to increase realism)</userdoc>
        public IMaterialTessellationFeature Tessellation { get; set; }

        /// <summary>
        /// Gets or sets the displacement.
        /// </summary>
        /// <value>The displacement.</value>
        /// <userdoc>The method used for displacement (altering vertex positions by adding offsets)</userdoc>
        public IMaterialDisplacementFeature Displacement { get; set; }

        /// <summary>
        /// Gets or sets the surface.
        /// </summary>
        /// <value>The surface.</value>
        /// <userdoc>The method used to alter macrosurface aspects (eg perturbing the normals of the model)</userdoc>
        public IMaterialSurfaceFeature Surface { get; set; }

        /// <summary>
        /// Gets or sets the micro surface.
        /// </summary>
        /// <value>The micro surface.</value>
        /// <userdoc>The method used to alter the material microsurface</userdoc>
        public IMaterialMicroSurfaceFeature MicroSurface { get; set; }
    }

    /// <summary>
    /// The material shading attributes define the color characteristics of the material and how it reacts to light.
    /// </summary>
    public class ShadingAttributes
    {
        /// <summary>
        /// Gets or sets the diffuse.
        /// </summary>
        /// <value>The diffuse.</value>
        /// <userdoc>The method used to determine the diffuse color of the material. 
        /// The diffuse color is the essential (pure) color of the object without reflections.</userdoc>
        public IMaterialDiffuseFeature Diffuse { get; set; }

        /// <summary>
        /// Gets or sets the diffuse model.
        /// </summary>
        /// <value>The diffuse model.</value>
        /// <userdoc>The shading model used to render the diffuse color.</userdoc>
        public IMaterialDiffuseModelFeature DiffuseModel { get; set; }

        /// <summary>
        /// Gets or sets the specular.
        /// </summary>
        /// <value>The specular.</value>
        /// <userdoc>The method used to determine the specular color. 
        /// This is the color produced by the reflection of a white light on the object.</userdoc>
        public IMaterialSpecularFeature Specular { get; set; }

        /// <summary>
        /// Gets or sets the specular model.
        /// </summary>
        /// <value>The specular model.</value>
        /// <userdoc>The shading model used to render the material specular color</userdoc>
        public IMaterialSpecularModelFeature SpecularModel { get; set; }

        /// <summary>
        /// Gets or sets the emissive.
        /// </summary>
        /// <value>The emissive.</value>
        /// <userdoc>The method used to determine the emissive color (the color emitted by the object)
        /// </userdoc>
        public IMaterialEmissiveFeature Emissive { get; set; }
        public IMaterialSubsurfaceScatteringFeature SubsurfaceScattering { get; set; }
    }

    /// <summary>
    /// The material misc attributes allow to set the occulsion, transparency and material layers.
    /// </summary>
    public class MiscAttributes
    {
        /// <summary>
        /// Gets or sets the occlusion.
        /// </summary>
        /// <value>The occlusion.</value>
        /// <userdoc>The occlusion method. Occlusions modulate the ambient and direct lighting of the material to simulate shadows or cavity artifacts.
        /// </userdoc>
        public IMaterialOcclusionFeature Occlusion { get; set; }

        /// <summary>
        /// Gets or sets the transparency.
        /// </summary>
        /// <value>The transparency.</value>
        /// <userdoc>The method used to determine the transparency</userdoc>
        public IMaterialTransparencyFeature Transparency { get; set; }

        /// <summary>
        /// Gets or sets the overrides.
        /// </summary>
        /// <value>The overrides.</value>
        /// <userdoc>Override properties of the current material</userdoc>
        public MaterialOverrides Overrides { get; set; }

        /// <summary>
        /// Gets or sets the cull mode used for the material.
        /// </summary>
        /// <userdoc>Cull some faces of the model depending on orientation</userdoc>
        public CullMode CullMode { get; set; }

        /// <summary>
        /// Gets or sets the clear coat shading features for the material.
        /// </summary>
        /// <userdoc>Use clear-coat shading to simulate vehicle paint</userdoc>
        public IMaterialClearCoatFeature ClearCoat { get; set; }
    }
}

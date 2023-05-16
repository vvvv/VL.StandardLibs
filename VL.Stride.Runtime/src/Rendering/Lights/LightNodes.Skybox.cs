using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect.GGXPrefiltering;
using Stride.Rendering.ComputeEffect.LambertianPrefiltering;
using Stride.Rendering.Materials;
using Stride.Rendering.Skyboxes;
using Stride.Shaders;
using System;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Stride.Engine;
using ServiceRegistry = VL.Core.ServiceRegistry;

namespace VL.Stride.Rendering.Lights
{
    // Mostly taken from Strides SkyboxGenerator but stripped of any asset related stuff
    static partial class LightNodes
    {
        class SkyboxRenderer : global::Stride.Rendering.RendererBase
        {
            private readonly IResourceHandle<Game> gameHandle;
            private readonly SchedulerSystem schedulerSystem;
            private Texture cubeMap;
            private bool isSpecularOnly;
            private SkyboxPreFilteringDiffuseOrder diffuseSHOrder;
            private int specularCubeMapSize;
            private bool invalidated;
            private LambertianPrefilteringSHNoCompute lamberFiltering;
            private RadiancePrefilteringGGXNoCompute specularRadiancePrefilterGGX;

            public SkyboxRenderer(NodeContext nodeContext)
            {
                gameHandle = AppHost.Current.Services.GetGameHandle().DisposeBy(this);
                schedulerSystem = gameHandle.Resource.Services.GetService<SchedulerSystem>();

                Skybox = new Skybox();

                IsSpecularOnly = false;
                DiffuseSHOrder = SkyboxPreFilteringDiffuseOrder.Order3;
                SpecularCubeMapSize = 256;
            }

            /// <summary>
            /// Gets or sets the type of skybox.
            /// </summary>
            /// <value>The type of skybox.</value>
            /// <userdoc>The texture to use as skybox (eg a cubemap or panoramic texture)</userdoc>
            public Texture CubeMap
            {
                get => cubeMap;
                set
                {
                    if (value != cubeMap)
                    {
                        cubeMap = value;
                        Invalidate();
                    }
                }
            }

            /// <summary>
            /// Gets or set if this skybox affects specular only, if <c>false</c> this skybox will affect ambient lighting
            /// </summary>
            /// <userdoc>
            /// Use the skybox only for specular lighting
            /// </userdoc>
            public bool IsSpecularOnly
            {
                get => isSpecularOnly;
                set
                {
                    if (value != isSpecularOnly)
                    {
                        isSpecularOnly = value;
                        Invalidate();
                    }
                }
            }

            /// <summary>
            /// Gets or sets the diffuse sh order.
            /// </summary>
            /// <value>The diffuse sh order.</value>
            /// <userdoc>The level of detail of the compressed skybox, used for diffuse lighting (dull materials). Order5 is more detailed than Order3.</userdoc>
            public SkyboxPreFilteringDiffuseOrder DiffuseSHOrder
            {
                get => diffuseSHOrder;
                set
                {
                    if (value != diffuseSHOrder)
                    {
                        diffuseSHOrder = value;
                        Invalidate();
                    }
                }
            }

            /// <summary>
            /// Gets or sets the specular cubemap size
            /// </summary>
            /// <value>The specular cubemap size.</value>
            /// <userdoc>The cubemap size used for specular lighting. Larger cubemap have more detail.</userdoc>
            public int SpecularCubeMapSize
            {
                get => specularCubeMapSize;
                set
                {
                    if (value != specularCubeMapSize)
                    {
                        specularCubeMapSize = value;
                        Invalidate();
                    }
                }
            }

            /// <summary>
            /// Forces a re-build of the skybox environment map.
            /// By default the skybox will only be built when one of its parameters changes.
            /// </summary>
            public bool ForceBuild { get; set; }

            public Skybox Skybox { get; }

            public void ScheduleForRendering()
            {
                if (CubeMap is null)
                {
                    DisposeCurrentCubeMap();
                    return;
                }

                if (invalidated || ForceBuild)
                {
                    invalidated = false;
                    schedulerSystem.Schedule(this);
                }
            }

            protected override void Destroy()
            {
                DisposeCurrentCubeMap();
                base.Destroy();
            }

            void Invalidate()
            {
                invalidated = true;
            }

            void DisposeCurrentCubeMap()
            {
                var cubeTexture = Skybox.SpecularLightingParameters.Get(SkyboxKeys.CubeMap);
                cubeTexture?.Dispose();
                Skybox.SpecularLightingParameters.Set(SkyboxKeys.CubeMap, null);
            }

            Texture GetCubeTexture(RenderDrawContext context, int textureSize, PixelFormat filteringTextureFormat)
            {
                var cubeTexture = Skybox.SpecularLightingParameters.Get(SkyboxKeys.CubeMap);
                if (cubeTexture != null)
                {
                    if (cubeTexture.Width != textureSize || cubeTexture.Format != filteringTextureFormat)
                    {
                        cubeTexture.Dispose();
                        cubeTexture = null;
                    }    
                }
                if (cubeTexture is null)
                {
                    cubeTexture = Texture.NewCube(context.GraphicsDevice, textureSize, true, filteringTextureFormat);
                }
                return cubeTexture;
            }

            protected override void InitializeCore()
            {
                lamberFiltering = new LambertianPrefilteringSHNoCompute(Context).DisposeBy(this);
                specularRadiancePrefilterGGX = new RadiancePrefilteringGGXNoCompute(Context).DisposeBy(this);
                base.InitializeCore();
            }

            protected override void DrawCore(RenderDrawContext context)
            {
                if (CubeMap is null)
                    return;

                // load the skybox texture from the asset.
                var skyboxTexture = CubeMap;
                if (skyboxTexture.ViewDimension == TextureDimension.Texture2D)
                {
                    var cubemapSize = (int)Math.Pow(2, Math.Ceiling(Math.Log(skyboxTexture.Width / 4) / Math.Log(2))); // maximum resolution is around horizontal middle line which composes 4 images.
                    // TODO: Use temporary texture
                    skyboxTexture = CubemapFromTextureRenderer.GenerateCubemap(Services, context, skyboxTexture, cubemapSize);
                }
                else if (skyboxTexture.ViewDimension != TextureDimension.TextureCube)
                {
                    throw new ArgumentException($"SkyboxGenerator: The texture type ({skyboxTexture.ViewDimension}) used as skybox is not supported. Should be a Cubemap or a 2D texture.");
                }

                // If we are using the skybox asset for lighting, we can compute it
                // Specular lighting only?
                if (!IsSpecularOnly)
                {
                    // -------------------------------------------------------------------
                    // Calculate Diffuse prefiltering
                    // -------------------------------------------------------------------
                    lamberFiltering.HarmonicOrder = (int)DiffuseSHOrder;
                    lamberFiltering.RadianceMap = skyboxTexture;
                    lamberFiltering.Draw(context);

                    var coefficients = lamberFiltering.PrefilteredLambertianSH.Coefficients;
                    for (int i = 0; i < coefficients.Length; i++)
                    {
                        coefficients[i] = coefficients[i] * SphericalHarmonics.BaseCoefficients[i];
                    }

                    Skybox.DiffuseLightingParameters.Set(SkyboxKeys.Shader, new ShaderClassSource("SphericalHarmonicsEnvironmentColor", lamberFiltering.HarmonicOrder));
                    Skybox.DiffuseLightingParameters.Set(SphericalHarmonicsEnvironmentColorKeys.SphericalColors, coefficients);
                }

                // -------------------------------------------------------------------
                // Calculate Specular prefiltering
                // -------------------------------------------------------------------
                var textureSize = SpecularCubeMapSize <= 0 ? 64 : SpecularCubeMapSize;
                textureSize = (int)Math.Pow(2, Math.Round(Math.Log(textureSize, 2)));
                if (textureSize < 64) textureSize = 64;

                // TODO: Add support for HDR 32bits 
                var filteringTextureFormat = skyboxTexture.Format.IsHDR() ? skyboxTexture.Format : PixelFormat.R8G8B8A8_UNorm;

                //var outputTexture = Texture.New2D(graphicsDevice, 256, 256, skyboxTexture.Format, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess, 6);
                var outputTexture = PushScopedResource(context.GraphicsContext.Allocator.GetTemporaryTexture2D(textureSize, textureSize, filteringTextureFormat, true, arraySize: 6));
                {
                    specularRadiancePrefilterGGX.RadianceMap = skyboxTexture;
                    specularRadiancePrefilterGGX.PrefilteredRadiance = outputTexture;
                    specularRadiancePrefilterGGX.Draw(context);

                    var cubeTexture = GetCubeTexture(context, textureSize, filteringTextureFormat);
                    context.CommandList.Copy(outputTexture, cubeTexture);

                    Skybox.SpecularLightingParameters.Set(SkyboxKeys.Shader, new ShaderClassSource("RoughnessCubeMapEnvironmentColor"));
                    Skybox.SpecularLightingParameters.Set(SkyboxKeys.CubeMap, cubeTexture);
                }

                if (skyboxTexture != CubeMap)
                    skyboxTexture.Dispose();
            }
        }
    }
}
// Much of this code is based on Stride's VideoTexture class.

using Stride.Core;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Stride.Engine;
using ServiceRegistry = VL.Core.ServiceRegistry;

namespace VL.Stride.Rendering
{
    /// <summary>
    /// Generates a texture with the desired amount of mipmaps for a given input texture.
    /// </summary>
    class MipMapGenerator : global::Stride.Rendering.RendererBase
    {
        private readonly List<Texture> renderTargetMipMaps = new List<Texture>();
        private readonly IResourceHandle<GraphicsDevice> graphicsDeviceHandle;
        private readonly SchedulerSystem schedulerSystem;
        private SamplerState minMagLinearMipPointSampler;
        private EffectInstance effectTexture2DCopy;
        private Texture inputTexture, outputTexture;
        private int maxMipMapCount;

        public MipMapGenerator(NodeContext nodeContext)
        {
            graphicsDeviceHandle = AppHost.Current.Services.GetDeviceHandle().DisposeBy(this);
            var gameHandle = AppHost.Current.Services.GetGameHandle().DisposeBy(this);
            schedulerSystem = gameHandle.Resource.Services.GetService<SchedulerSystem>();
        }

        /// <summary>
        /// The input texture.
        /// </summary>
        public Texture InputTexture
        {
            get => inputTexture;
            set
            {
                if (value != inputTexture)
                {
                    inputTexture = value;
                    DeallocateOutput();
                }
            }
        }

        /// <summary>
        /// The maximum amount of mipmaps to generate. Use zero to generate all.
        /// </summary>
        public int MaxMipMapCount
        {
            get => maxMipMapCount;
            set
            {
                if (value != maxMipMapCount)
                {
                    maxMipMapCount = value;
                    DeallocateOutput();
                }
            }
        }

        /// <summary>
        /// The output texture with the generated mipmaps.
        /// </summary>
        public Texture OutputTexture
        {
            get => outputTexture ??= AllocateOutputTexture(InputTexture);
        }

        /// <summary>
        /// Places this renderer in the rendering queue.
        /// </summary>
        public void ScheduleForRendering()
        {
            schedulerSystem.Schedule(this);
        }

        private Texture AllocateOutputTexture(Texture inputTexture)
        {
            if (inputTexture is null)
                return null;

            var width = inputTexture.Width;
            var height = inputTexture.Height;

            // Calculate the optimum amount of mip maps
            var mipMapCount = Texture.CountMips(width, height);
            // Clamp it to user maximum if provided
            if (MaxMipMapCount > 0)
                mipMapCount = Math.Min(mipMapCount, MaxMipMapCount);

            var textureDescription = TextureDescription.New2D(width, height, mipMapCount, inputTexture.ViewFormat, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            var renderTarget = Texture.New(graphicsDeviceHandle.Resource, textureDescription);
            AllocateTextureViewsForMipMaps(renderTarget);
            return renderTarget;
        }

        private void AllocateTextureViewsForMipMaps(Texture parentTexture)
        {
            DeallocateTextureViewsForMipMaps();

            for (int i = 0; i < parentTexture.MipLevels; ++i)
            {
                var renderTargetMipMapTextureViewDescription = new TextureViewDescription
                {
                    Type = ViewType.Single,
                    MipLevel = i,
                    Format = parentTexture.ViewFormat,
                    ArraySlice = 0,
                    Flags = parentTexture.Flags,
                };

                Texture renderTargetMipMapTextureView = parentTexture.ToTextureView(renderTargetMipMapTextureViewDescription);
                renderTargetMipMaps.Add(renderTargetMipMapTextureView);
            }
        }

        private void DeallocateOutput()
        {
            DeallocateTextureViewsForMipMaps();
            outputTexture?.Dispose();
            outputTexture = null;
        }

        private void DeallocateTextureViewsForMipMaps()
        {
            foreach (var mipmap in renderTargetMipMaps)
                mipmap.Dispose();

            renderTargetMipMaps.Clear();
        }

        protected override void InitializeCore()
        {
            // We want to sample mip maps using point filtering (to make sure nothing bleeds between mip maps).
            minMagLinearMipPointSampler = SamplerState.New(GraphicsDevice, new SamplerStateDescription(TextureFilter.MinMagLinearMipPoint, TextureAddressMode.Clamp))
                .DisposeBy(this);

            // Allocate the effect for copying regular 2d textures:
            effectTexture2DCopy = new EffectInstance(EffectSystem.LoadEffect("SpriteEffectExtTextureRegular").WaitForResult())
                .DisposeBy(this);
            effectTexture2DCopy.Parameters.Set(SpriteEffectExtTextureRegularKeys.MipLevel, 0.0f);
            effectTexture2DCopy.Parameters.Set(SpriteEffectExtTextureRegularKeys.Sampler, minMagLinearMipPointSampler);
            effectTexture2DCopy.UpdateEffect(GraphicsDevice);

            base.InitializeCore();
        }

        protected override void Destroy()
        {
            DeallocateOutput();
            base.Destroy();
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            if (InputTexture is null)
                return;

            var graphicsContext = context.GraphicsContext;

            using (context.PushRenderTargetsAndRestore())
            {
                CopyTexture(graphicsContext,
                            effectTexture2DCopy,
                            InputTexture,
                            renderTargetMipMaps[0]); // Set the highest mip map level as the render target.

                // Generate mip maps (start from level 1 because we just generated  level 0 above):
                for (int i = 1; i < renderTargetMipMaps.Count; ++i)
                {
                    CopyTexture(graphicsContext,
                                effectTexture2DCopy,
                                renderTargetMipMaps[i - 1], // Use the parent mip map level as the input texture.
                                renderTargetMipMaps[i]); // Set the child mip map level as the render target.
                }
            }
        }

        private static void CopyTexture(GraphicsContext graphicsContext, EffectInstance effectInstance, Texture input, Texture output)
        {
            // Set the "input" texture as the texture that we will copy to "output":
            effectInstance.Parameters.Set(SpriteEffectExtTextureRegularKeys.TextureRegular, input); // TODO: STABILITY: Supply the parent texture instead? I mean here we're using SampleLOD in the shader because texture views are basically being ignored during sampling on OpenGL/ES.

            // Set the mipmap level of the input texture we want to sample:
            effectInstance.Parameters.Set(SpriteEffectExtTextureRegularKeys.MipLevel, input.MipLevel);  // TODO: STABILITY: Manually pass the mip level?

            // Set the "output" texture as the render target (the copy destination):
            graphicsContext.CommandList.SetRenderTargetAndViewport(null, output);

            // Perform the actual draw call to filter and copy the texture:
            graphicsContext.DrawQuad(effectInstance);
        }
    }
}

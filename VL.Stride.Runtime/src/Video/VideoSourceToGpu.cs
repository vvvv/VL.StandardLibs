#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Core.Mathematics;
using Stride.Shaders;
using VL.Core;
using VL.Lib.Basics.Imaging;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using VL.Lib.Video;
using VL.Stride.Shaders.ShaderFX;
using GpuColor = VL.Stride.Shaders.ShaderFX.SetVar<Stride.Core.Mathematics.Color4>;

namespace VL.Stride.Video;

/// <summary>
/// Describes the video texture format and holds the texture handle.
/// Created on the producer thread.
/// </summary>
internal sealed class VideoTextureInfo : IDisposable
{
    public IResourceHandle<Texture?> TextureHandle { get; }
    public VL.Lib.Basics.Imaging.PixelFormat PixelFormat { get; }
    public int PlaneCount { get; }

    // Shader created on producer thread - just object creation, no render dependencies
    public VideoFormatShader FormatShader { get; }

    public VideoTextureInfo(
        IResourceHandle<Texture?> textureHandle, 
        VL.Lib.Basics.Imaging.PixelFormat pixelFormat, 
        int planeCount,
        VideoFormatShader formatShader)
    {
        TextureHandle = textureHandle;
        PixelFormat = pixelFormat;
        PlaneCount = planeCount;
        FormatShader = formatShader;
    }

    public void Dispose()
    {
        TextureHandle?.Dispose();
    }
}

/// <summary>
/// Holds the compiled shader state for a specific video format.
/// Created and used on the render thread.
/// </summary>
internal sealed class VideoFormatShader
{
    // Only the texture is dynamic - updated per frame
    public ObjectParameterUpdater<Texture> TextureUpdater { get; } = new();
    public ObjectParameterKey<Texture>? TextureKey { get; set; }

    // Sampler is static - part of the shader strategy
    public SamplerState Sampler { get; set; } = null!;
    public ObjectParameterKey<SamplerState>? SamplerKey { get; set; }

    // Cached shader graph
    public GpuColor ShaderGraph { get; set; } = null!;

    // Format info to identify if shader needs rebuilding
    public VL.Lib.Basics.Imaging.PixelFormat PixelFormat { get; set; }
    public int PlaneCount { get; set; }
}

[ProcessNode]
public sealed class VideoSourceToGpu : IDisposable
{
    private readonly RenderContext renderContext;
    private readonly VideoPlaybackContext ctx;

    // Internal source that provides VideoTextureInfo
    private readonly InternalVideoTextureSource internalSource;

    public VideoSourceToGpu(NodeContext nodeContext)
    {
        var appHost = AppHost.Current;
        renderContext = RenderContext.GetShared(appHost.Services.GetRequiredService<Game>().Services);
        ctx = VideoUtils.CreatePlaybackContext(nodeContext);

        internalSource = new InternalVideoTextureSource(renderContext, ctx);
    }

    [return: Pin(Name = "Output")]
    public GpuColor Update(IVideoSource? videoSource, GpuColor fallback, bool mipmapped = false, bool preferPush = true, bool preferGpu = true)
    {
        // Get the video texture info from internal source (shader state already created on producer thread)
        var videoTextureInfo = internalSource.Update(videoSource, null, mipmapped, preferPush, preferGpu);

        if (videoTextureInfo == null)
            return fallback;

        // Update only the dynamic texture parameter
        var formatShader = videoTextureInfo.FormatShader;
        if (videoTextureInfo.TextureHandle.Resource != null)
        {
            formatShader.TextureUpdater.Value = videoTextureInfo.TextureHandle.Resource;
        }

        return formatShader.ShaderGraph;
    }



    public void Dispose()
    {
        internalSource?.Dispose();
    }

    /// <summary>
    /// Internal implementation that converts video frames to textures and wraps them in VideoTextureInfo.
    /// Runs on producer thread.
    /// </summary>
    private sealed class InternalVideoTextureSource : VideoSourceToImage<VideoTextureInfo?>
    {
        private readonly RenderContext renderContext;
        private readonly VideoPlaybackContext ctx;

        // Cache shader states by format to avoid recreation
        private readonly Dictionary<(VL.Lib.Basics.Imaging.PixelFormat, int), VideoFormatShader> shaderStateCache = new();

        public InternalVideoTextureSource(RenderContext renderContext, VideoPlaybackContext ctx)
        {
            this.renderContext = renderContext;
            this.ctx = ctx;
        }

        protected override VideoPlaybackContext Context => ctx;

        protected override IResourceHandle<VideoTextureInfo?>? GetHandle(IResourceProvider<VideoFrame> videoFrameProvider, bool mipmapped)
        {
            using var videoFrameHandle = videoFrameProvider.GetHandle();
            var videoFrame = videoFrameHandle.Resource;

            // Convert to texture
            var textureProvider = videoFrameProvider.ToTexture(renderContext);
            var textureHandle = textureProvider.GetHandle();

            // Get or create shader state for this format (producer thread)
            var formatKey = (videoFrame.PixelFormat, videoFrame.PlaneCount);
            if (!shaderStateCache.TryGetValue(formatKey, out var formatShader))
            {
                formatShader = CreateShaderForFormat(videoFrame.PixelFormat, videoFrame.PlaneCount);
                shaderStateCache[formatKey] = formatShader;
            }

            // Create VideoTextureInfo with format information and shader state
            var videoTextureInfo = new VideoTextureInfo(
                textureHandle,
                videoFrame.PixelFormat,
                videoFrame.PlaneCount,
                formatShader
            );

            return ResourceProvider.Return(
                videoTextureInfo,
                videoTextureInfo,
                info => info.Dispose()).GetHandle();
        }

        private VideoFormatShader CreateShaderForFormat(VL.Lib.Basics.Imaging.PixelFormat pixelFormat, int planeCount)
        {
            var state = new VideoFormatShader
            {
                PixelFormat = pixelFormat,
                PlaneCount = planeCount
            };

            // Create static sampler for this format
            var samplerStateDescription = SamplerStateDescription.Default;
            samplerStateDescription.Filter = TextureFilter.Linear;
            state.Sampler = SamplerState.New(renderContext.GraphicsDevice, samplerStateDescription);

            // Determine which shader to use based on pixel format
            string shaderName = pixelFormat switch
            {
                VL.Lib.Basics.Imaging.PixelFormat.UYVY => "SampleVideoTextureUYVY",
                VL.Lib.Basics.Imaging.PixelFormat.B8G8R8A8 => "SampleVideoTextureBGRA",
                _ => throw new NotSupportedException($"Pixel format {pixelFormat} is not yet supported for GPU video playback")
            };

            // Create a simple shader node that samples the video texture
            // The GenericComputeNode lambda gets called during shader compilation to generate the shader source
            var sampledColor = new GenericComputeNode<Color4>(
                (context, baseKeys) =>
                {
                    // Get or create parameter key for the dynamic texture
                    state.TextureKey = context.GetKeyForContext(state.TextureUpdater, state.TextureKey);

                    // Track only the dynamic texture parameter
                    state.TextureUpdater.Track(context, state.TextureKey);

                    // Determine resource group based on context type
                    var resourceGroup = context is MaterialGeneratorContext ? "PerMaterial" : "PerUpdate";

                    // Create a fixed parameter key for the static sampler (only once)
                    // The key name will be automatically generated as "{resourceGroup}.VideoSampler" by Stride
                    state.SamplerKey ??= ParameterKeys.NewObject<SamplerState>();

                    // Set the static sampler parameter
                    context.Parameters.Set(state.SamplerKey, state.Sampler);

                    // Reference our SDSL shader with only the texture parameter
                    // The shader directly reads texture coordinates from streams.TexCoord (TEXCOORD0 semantic)
                    return new ShaderClassSource(
                        shaderName,
                        state.TextureKey.Name,      // TextureName (LinkType)
                        resourceGroup               // TextureResourceGroupName (MemberName)
                    );
                },
                inputs: null  // No inputs needed - shader reads directly from streams.TexCoord
            );

            // Create variable declaration and assignment
            var colorDecl = new DeclVar<Color4>("VideoColor");
            state.ShaderGraph = new SetVar<Color4>(sampledColor, colorDecl);

            return state;
        }
    }
}
#nullable restore

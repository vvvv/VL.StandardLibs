#nullable enable

using SkiaSharp;
using Stride.Core.Mathematics;
using Stride.Graphics;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using VL.Core;
using VL.Skia;
using VL.Skia.Egl;
using VL.Stride;
using VL.Stride.Games;
using VL.Stride.Graphics;

[assembly: ImportType(typeof(SkiaTexture), Name = "SkiaTexture")]

namespace VL.Stride;

/// <summary>
/// Renders Skia content into Stride textures.
/// </summary>
[ProcessNode]
[SupportedOSPlatform("Windows6.1")]
public unsafe sealed partial class SkiaTexture : IDisposable
{
    private readonly AppHost appHost;
    private readonly VLGame game;
    private readonly SerialDisposable appHostSubscription = new();
    private readonly SetSpaceUpstream2 withinCommonSpace = new();
    private readonly ILogger logger;

    private bool clear;
    private Color4 clearColor;
    private Int2 size;
    private PixelFormat format;
    private bool useDedicatedDevice;
    private Texture? strideTexture;
    private Renderer? renderer;

    private EglSurface? eglSurface;
    private SKSurface? skSurface;
    private CallerInfo? callerInfo;

    public SkiaTexture(NodeContext nodeContext)
    {
        appHost = nodeContext.AppHost;
        game = appHost.Services.GetRequiredService<VLGame>();
        logger = nodeContext.GetLogger();
    }

    public void Dispose()
    {
        DisposeInstanceResources();
        appHostSubscription.Dispose();
    }

    /// <summary>
    /// Renders a Skia layer to a Stride texture.
    /// </summary>
    /// <param name="input">The Skia layer to render.</param>
    /// <param name="size">The size of the output texture.</param>
    /// <param name="space">The coordinate space to use.</param>
    /// <param name="clear">Whether to clear the texture before rendering.</param>
    /// <param name="clearColor">The color to use when clearing.</param>
    /// <param name="format">The pixel format of the output texture.</param>
    /// <param name="useDedicatedDevice">Whether to use a dedicated D3D11 device for rendering. Slightly reduced performance but might help stability on some hardware.</param>
    /// <returns>The rendered Stride texture.</returns>
    [return: Pin(Name = "Output")]
    public Texture? Update(
        ILayer input,
        [DefaultValue(typeof(Int2), "1280,720")] Int2 size,
        CommonSpace space,
        [DefaultValue(true)] bool clear,
        [DefaultValue(typeof(Color4), "#FF000000")] Color4 clearColor,
        [DefaultValue(typeof(PixelFormat), nameof(PixelFormat.B8G8R8A8_UNorm_SRgb))] PixelFormat format,
        [Pin(Visibility = Model.PinVisibility.Hidden)] bool useDedicatedDevice /* Let's only enable this again when we actually conclude that the device context state switch is problematic */)
    {
        if (size != this.size || format != this.format || useDedicatedDevice != this.useDedicatedDevice || renderer is null)
        {
            this.size = size;
            this.format = format;
            this.useDedicatedDevice = useDedicatedDevice;
            DisposeInstanceResources();
            CreateInstanceResources(new Size2(Math.Max(1, size.X), Math.Max(1, size.Y)), shared: useDedicatedDevice);
        }

        this.clear = clear;
        this.clearColor = clearColor;

        withinCommonSpace.Update(input, out var layer, space);

        // Register for rendering
        if (renderer != null && eglSurface != null && skSurface != null)
            renderer.Schedule(this);

        return strideTexture;
    }

    internal void Render(RenderContext renderContext)
    {
        try
        {
            // Make our surface default FBO
            renderContext.MakeCurrent(eglSurface!);

            var canvas = skSurface!.Canvas;
            if (clear)
                canvas.Clear(ToSKColor(ref clearColor));
            withinCommonSpace.Render(callerInfo!);

            skSurface.Flush();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while rendering");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static SKColor ToSKColor(ref Color4 input)
    {
        input.ToBgra(out var r, out var g, out var b, out var a);
        return new SKColor(r, g, b, a);
    }

    private void CreateInstanceResources(Size2 size, bool shared)
    {
        var strideDevice = game.GraphicsDevice;
        var isLinear = strideDevice.ColorSpace == ColorSpace.Linear;
        var textureFormat = format;
        if (isLinear && format.IsSRgb() && format.TryToTypeless(out var typelessFormat))
            textureFormat = typelessFormat;
        strideTexture = Texture.New(strideDevice,
            description: new TextureDescription()
            {
                Width = size.Width,
                Height = size.Height,
                ArraySize = 1,
                MipLevels = 1,
                MultisampleCount = MultisampleCount.None,
                Format = textureFormat,
                Dimension = TextureDimension.Texture2D,
                Usage = GraphicsResourceUsage.Default,
                Flags = TextureFlags.ShaderResource | TextureFlags.RenderTarget,
                Options = shared ? TextureOptions.Shared : TextureOptions.None
            },
            viewDescription: new TextureViewDescription()
            {
                Flags = TextureFlags.ShaderResource | TextureFlags.RenderTarget,
                Format = format,
                Type = ViewType.Full,
            });

        // Select renderer based on strategy
        renderer = shared ? DedicatedRenderer.GetOrCreate(appHost, game) : DefaultRenderer.GetOrCreate(appHost, game);
        var renderContextProvider = renderer.RenderContextProvider;
        appHostSubscription.Disposable = renderContextProvider.OnDeviceLost
            .Subscribe(_ => DisposeInstanceResources());

        var renderContext = renderContextProvider.GetRenderContext();
        eglSurface = renderer.CreateEglSurface(strideTexture);

        // The EGL surface will be bound as default FBO when rendering -> FBO ID 0
        var skColorType = format.ToSKColorType();
        var glInfo = new GRGlFramebufferInfo(fboId: 0, skColorType.ToGlSizedFormat());
        using var gRBackendRenderTarget = new GRBackendRenderTarget(size.Width, size.Height, 0, 0, glInfo);

        var colorSpace = isLinear && format == textureFormat ? SKColorSpace.CreateSrgbLinear() : SKColorSpace.CreateSrgb();
        skSurface = SKSurface.Create(renderContext.SkiaContext, gRBackendRenderTarget, GRSurfaceOrigin.TopLeft, skColorType, colorSpace);
        callerInfo = skSurface != null ? CallerInfo.InRenderer(size.Width, size.Height, skSurface.Canvas, renderContext.SkiaContext) : null;
    }

    private void DisposeInstanceResources()
    {
        appHostSubscription.Dispose();

        callerInfo = null;
        Dispose(ref skSurface);
        Dispose(ref eglSurface);
        Dispose(ref strideTexture);

        static void Dispose<T>(ref T? r) where T : class, IDisposable
        {
            if (r != null)
            {
                r.Dispose();
                r = null;
            }
        }
    }
}
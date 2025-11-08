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

[assembly: ImportType(typeof(SkiaTexture), Name = "SkiaTexture_NEW")]

namespace VL.Stride;

/// <summary>
/// Renders Skia content into Stride textures.
/// </summary>
/// <remarks>
/// Has option to use a dedicated D3D11 device for Skia rendering to avoid conflicts with Stride's device state.
/// </remarks>
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
    private bool useDedicatedDevice;
    private Texture? strideTexture;
    private Renderer? renderer;

    internal EglSurface? eglSurface;
    internal SKSurface? skSurface;

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
    /// <param name="useDedicatedDevice">Whether to use a dedicated D3D11 device for rendering. Slightly reduced performance but might help stability on some hardware.</param>
    /// <returns>The rendered Stride texture.</returns>
    [return: Pin(Name = "Output")]
    public Texture? Update(
        ILayer input,
        [DefaultValue(typeof(Int2), "1280,720")] Int2 size,
        CommonSpace space,
        [DefaultValue(true)] bool clear,
        [DefaultValue(typeof(Color4), "#FF000000")] Color4 clearColor,
        bool useDedicatedDevice)
    {
        if (size != this.size || useDedicatedDevice != this.useDedicatedDevice || strideTexture is null)
        {
            this.size = size;
            this.useDedicatedDevice = useDedicatedDevice;
            DisposeInstanceResources();
            CreateInstanceResources(size, shared: useDedicatedDevice);
        }

        this.clear = clear;
        this.clearColor = clearColor;

        withinCommonSpace.Update(input, out var layer, space);

        // Register for rendering
        renderer?.Schedule(this);

        return strideTexture;
    }

    internal void Render(GRContext grContext, SKSurface renderTarget)
    {
        try
        {
            var canvas = renderTarget.Canvas;
            var callerInfo = CallerInfo.InRenderer(size.X, size.Y, canvas, grContext);
            if (clear)
                canvas.Clear(ToSKColor(ref clearColor));
            withinCommonSpace.Render(callerInfo);

            renderTarget.Flush();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error rendering Skia layer");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static SKColor ToSKColor(ref Color4 input)
    {
        input.ToBgra(out var r, out var g, out var b, out var a);
        return new SKColor(r, g, b, a);
    }

    private void CreateInstanceResources(Int2 size, bool shared)
    {
        var strideDevice = game.GraphicsDevice;
        strideTexture = Texture.New(strideDevice,
            description: new TextureDescription()
            {
                Width = size.X,
                Height = size.Y,
                ArraySize = 1,
                MipLevels = 1,
                MultisampleCount = MultisampleCount.None,
                Format = PixelFormat.B8G8R8A8_Typeless,
                Dimension = TextureDimension.Texture2D,
                Usage = GraphicsResourceUsage.Default,
                Flags = TextureFlags.ShaderResource | TextureFlags.RenderTarget,
                Options = shared ? TextureOptions.Shared : TextureOptions.None
            },
            viewDescription: new TextureViewDescription()
            {
                Flags = TextureFlags.ShaderResource | TextureFlags.RenderTarget,
                Format = strideDevice.ColorSpace == ColorSpace.Linear ? PixelFormat.B8G8R8A8_UNorm_SRgb : PixelFormat.B8G8R8A8_UNorm,
                Type = ViewType.Full,
            });

        // Select renderer based on strategy
        renderer = shared ? DedicatedRenderer.GetOrCreate(appHost, game) : DefaultRenderer.GetOrCreate(appHost, game);
        appHostSubscription.Disposable = renderer.RenderContextProvider.OnDeviceLost
            .Subscribe(_ => DisposeInstanceResources());

        renderer.CreateSkiaSurfaces(strideTexture, out eglSurface, out skSurface);
    }

    private void DisposeInstanceResources()
    {
        appHostSubscription.Dispose();

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
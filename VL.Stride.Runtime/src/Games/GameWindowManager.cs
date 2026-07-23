using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using System.ComponentModel;
using VL.Core;
using VL.Lib.Reactive;
using VL.Stride.Games;
using VL.Stride.Rendering;

[assembly: ImportType(typeof(GameWindowManager), Category = "Stride.Windowing.Internal")]

namespace VL.Stride.Games;

[ProcessNode(FragmentSelection = FragmentSelection.Explicit, HasStateOutput = true)]
public sealed class GameWindowManager : IDisposable
{
    private readonly GameWindowRenderer gameWindowRenderer;
    private readonly ILogger logger;

    [Fragment]
    public GameWindowManager(
        NodeContext nodeContext,
        RectangleF boundsInPixels,
        bool isBorderless,
        IChannel<bool> alwaysOnTop,
        IChannel<bool> extendIntoTitleBar,
        MultisampleCount multisampleCount,
        [DefaultValue(PixelFormat.R16G16B16A16_Float)] PixelFormat backBufferFormat,
        [DefaultValue(PixelFormat.D24_UNorm_S8_UInt)] PixelFormat depthBufferFormat = PixelFormat.D24_UNorm_S8_UInt,
        [DefaultValue(GraphicsProfile.Level_11_0)] GraphicsProfile graphicsProfile = GraphicsProfile.Level_11_0,
        [DefaultValue(ColorSpaceType.RgbFullG22NoneP709)] ColorSpaceType preferredOutputColorSpace = ColorSpaceType.RgbFullG22NoneP709,
        int inputPriority = 0)
    {
        logger = nodeContext.GetLogger();
        var game = nodeContext.AppHost.Services.GetRequiredService<Game>();
        var gameContext = VLGame.CreateGameContext(nodeContext, alwaysOnTop, extendIntoTitleBar, game.Context.ContextType, (int)boundsInPixels.Width, (int)boundsInPixels.Height, isUserManagingRun: true);
        gameWindowRenderer = new GameWindowRenderer(game.Services, gameContext, inputPriority);
        var windowManager = gameWindowRenderer.WindowManager;
        windowManager.PreferredBackBufferWidth = (int)boundsInPixels.Width;
        windowManager.PreferredBackBufferHeight = (int)boundsInPixels.Height;
        windowManager.PreferredBackBufferFormat = backBufferFormat;
        windowManager.PreferredDepthStencilFormat = depthBufferFormat;
        windowManager.PreferredOutputColorSpace = preferredOutputColorSpace;
        windowManager.ShaderProfile = graphicsProfile;
        windowManager.PreferredGraphicsProfile = [graphicsProfile];
        windowManager.PreferredColorSpace = game.GraphicsDevice.ColorSpace;
        windowManager.PreferredMultisampleCount = multisampleCount;
        gameWindowRenderer.Initialize();

        var window = gameWindowRenderer.Window;
        window.Position = new Int2((int)boundsInPixels.X, (int)boundsInPixels.Y);
        window.FullscreenIsBorderlessWindow = true;
        window.PreferredFullscreenSize = new Int2(-1, -1); // adapt to desktop size
        window.AllowUserResizing = true;
        window.IsMouseVisible = true;
        window.IsBorderLess = isBorderless;
        window.SetSize(new Int2((int)boundsInPixels.Width, (int)boundsInPixels.Height));
    }

    [Fragment]
    public GameWindow Window => gameWindowRenderer.Window;

    [Fragment]
    public IInputSource InputSource => gameWindowRenderer.InputSource;

    [Fragment]
    public Texture BackBuffer => gameWindowRenderer.Presenter.BackBuffer;

    [Fragment]
    public Texture DepthBuffer => gameWindowRenderer.Presenter.DepthStencilBuffer;

    public GameWindowRenderer GameWindowRenderer => gameWindowRenderer;

    [Fragment]
    public PresentInterval PresentInterval { set => gameWindowRenderer.Presenter.PresentInterval = value; }

    [Fragment]
    public IPresentCallIntercept PresentCallIntercept { set => gameWindowRenderer.PresentCallIntercept = value; }

    [Fragment]
    public ViewportSettings ViewportSettings { set => gameWindowRenderer.ViewportSettings = value; }

    [Fragment]
    public void SetOutputColorSpace(ColorSpaceType outputColorSpace, [DefaultValue(PixelFormat.B8G8R8A8_UNorm_SRgb)] PixelFormat backbufferFormat)
    {
        if (outputColorSpace != this.outputColorSpace || backbufferFormat != this.backbufferFormat)
        {
            gameWindowRenderer.WindowManager.PreferredOutputColorSpace = outputColorSpace;
            gameWindowRenderer.WindowManager.PreferredBackBufferFormat = backbufferFormat;
            try
            {
                gameWindowRenderer.Presenter.SetOutputColorSpace(outputColorSpace, backbufferFormat);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to set output color space to {outputColorSpace} with backbuffer format {backbufferFormat}");
            }
        }
    }
    ColorSpaceType outputColorSpace;
    PixelFormat backbufferFormat;


    public void Dispose()
    {
        gameWindowRenderer.Dispose();
    }
}

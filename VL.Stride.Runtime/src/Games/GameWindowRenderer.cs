// Modified version of Stride.Games.GameWindowRenderer using GameWindowRendererManager.
// This class should be kept internal

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;

namespace VL.Stride.Games
{
    /// <summary>
    /// A GameSystem that allows to draw to another window or control. Currently only valid on desktop with Windows.Forms.
    /// </summary>
    public class GameWindowRenderer : GameSystemBase
    {
        private GraphicsPresenter savedPresenter;
        private bool beginDrawOk;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameWindowRenderer" /> class.
        /// </summary>
        /// <param name="registry">The registry.</param>
        /// <param name="gameContext">The window context.</param>
        public GameWindowRenderer(IServiceRegistry registry, GameContext gameContext)
            : base(registry)
        {
            GameContext = gameContext;
            WindowManager = new GameWindowRendererManager();
        }

        /// <summary>
        /// Gets the underlying native window.
        /// </summary>
        /// <value>The underlying native window.</value>
        public GameContext GameContext { get; private set; }

        /// <summary>
        /// Gets the window.
        /// </summary>
        /// <value>The window.</value>
        public GameWindow Window { get; private set; }

        /// <summary>
        /// Gets or sets the presenter.
        /// </summary>
        /// <value>The presenter.</value>
        public GraphicsPresenter Presenter { get; protected set; }

        /// <summary>
        /// Gets the window manager.
        /// </summary>
        /// <value>
        /// The window manager.
        /// </value>
        public GameWindowRendererManager WindowManager { get; private set; }

        public override void Initialize()
        {
            var gamePlatform = Services.GetService<IGamePlatform>();
            //GameContext.RequestedWidth = WindowManager.PreferredBackBufferWidth;
            //GameContext.RequestedHeight = WindowManager.PreferredBackBufferHeight;
            Window = gamePlatform.CreateWindow(GameContext);
            Window.SetSize(new Int2(WindowManager.PreferredBackBufferWidth, WindowManager.PreferredBackBufferHeight));

            Window.Visible = true;
            

            base.Initialize();
        }

        protected override void Destroy()
        {
            if (Presenter != null)
            {
                // Make sure that the Presenter is reverted to window before shuting down
                // otherwise the Direct3D11.Device will generate an exception on Dispose()
                Presenter.IsFullScreen = false;
                Presenter.Dispose(); 
            }
            Presenter = null;

            WindowManager?.Dispose();
            WindowManager = null;
            Window?.Dispose();
            Window = null;

            base.Destroy();
        }

        protected virtual void EnsurePresenter()
        {
            if (Presenter == null)
            {
                var presentationParameters = new PresentationParameters(
                    WindowManager.PreferredBackBufferWidth,
                    WindowManager.PreferredBackBufferHeight,
                    Window.NativeWindow,
                    WindowManager.PreferredBackBufferFormat)
                {
                    DepthStencilFormat = WindowManager.PreferredDepthStencilFormat,
                    PresentationInterval = PresentInterval.Immediate,
                    MultisampleCount = WindowManager.PreferredMultisampleCount,
                };

#if STRIDE_GRAPHICS_API_DIRECT3D11 && STRIDE_PLATFORM_UWP
                if (Game.Context is GameContextUWPCoreWindow context && context.IsWindowsMixedReality)
                {
                    Presenter = new WindowsMixedRealityGraphicsPresenter(GraphicsDevice, presentationParameters);
                }
                else
#endif
                {
                    Presenter = new SwapChainGraphicsPresenter(GraphicsDevice, presentationParameters);
                }

                WindowManager.Initialize(this, GraphicsDevice, Services.GetService<IGraphicsDeviceFactory>());
            }
        }

        public override bool BeginDraw()
        {
            if (GraphicsDevice != null && Window.Visible)
            {
                savedPresenter = GraphicsDevice.Presenter;

                EnsurePresenter();

                GraphicsDevice.Presenter = Presenter;

                // Perform begin of frame presenter operations
                var commandList = Game.GraphicsContext.CommandList;
                if (Presenter.DepthStencilBuffer != null)
                    commandList.ResourceBarrierTransition(Presenter.DepthStencilBuffer, GraphicsResourceState.DepthWrite);
                if (Presenter.BackBuffer != null)
                    commandList.ResourceBarrierTransition(Presenter.BackBuffer, GraphicsResourceState.RenderTarget);

                Presenter.BeginDraw(commandList);

                beginDrawOk = true;
                return true;
            }

            beginDrawOk = false;
            return false;
        }

        public override void EndDraw()
        {
            if (beginDrawOk)
            {
                // We'd like to call Present() here like in the original code, however that would be too early
                // in case other game systems want to draw into our backbuffer (like GameProfilingSystem).
                // Present();
                var game = (VLGame)Game;
                game.PendingPresentCalls.Add(this);

                if (savedPresenter != null)
                {
                    GraphicsDevice.Presenter = savedPresenter;
                }
            }
        }

        // Called by VLGame at the very end to ensure that other game systems (like the GameProfilingSystem) were able to post their draw commands
        internal void Present()
        {
            if (beginDrawOk)
            {
                // Perform end of frame presenter operations
                var commandList = Game.GraphicsContext.CommandList;
                Presenter.EndDraw(commandList, present: true);

                if (Presenter.BackBuffer != null)
                    commandList.ResourceBarrierTransition(Presenter.BackBuffer, GraphicsResourceState.Present);

                try
                {
                    Presenter.Present();
                }
                catch (GraphicsException ex)
                {
                    if (ex.Status != GraphicsDeviceStatus.Removed && ex.Status != GraphicsDeviceStatus.Reset)
                    {
                        throw;
                    }
                }

                beginDrawOk = false;
            }
        }
    }
}

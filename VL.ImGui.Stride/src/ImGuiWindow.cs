

using Stride.Core.Mathematics;
using System.Runtime.InteropServices;

using Stride.Games;
using Stride.Graphics;

using VL.Core;
using VL.Stride;
using VL.Stride.Games;
using ImGuiNET;
using Stride.Input;
using Stride.Rendering;
using VL.Stride.Rendering;
using Stride.Engine;
using VL.Lib.Collections;
using VL.Stride.Engine;
using Stride.Rendering.Compositing;

namespace VL.ImGui.Stride
{
    using GameWindowRenderer = VL.Stride.Games.GameWindowRenderer;

    internal class ImGuiWindow : IDisposable
    {
        private readonly GCHandle _gcHandle;
        private readonly SchedulerSystem _schedulerSystem;
        private readonly GameWindowRenderer _gameWindowRenderer;
        private readonly IInputSource _inputSource;
        private readonly WindowRenderer _windowRenderer;

        public IntPtr Handle => _gameWindowRenderer.Window.NativeWindow.Handle;

        public Int2 Position
        {
            get { return _gameWindowRenderer.Window.Position; }
            set { _gameWindowRenderer.Window.Position = value; }
        }

        public Int2 Size
        {
            get { return new Int2(_gameWindowRenderer.Window.ClientBounds.Width, _gameWindowRenderer.Window.ClientBounds.Height); }
            set { _gameWindowRenderer.Window.SetSize(value); }
        }

        public string Title
        {
            get { return _gameWindowRenderer.Window.Title; }
            set { _gameWindowRenderer.Window.Title = value; }
        }

        public bool IsActivated
        {
            get { return _gameWindowRenderer.Window.IsActivated; }
        }

        public void Activate()
        {
            _gameWindowRenderer.Window.BringToFront();
        }

        public bool IsMinimized
        {
            get { return _gameWindowRenderer.Window.IsMinimized; }
        }

        public ImGuiWindow(NodeContext nodeContext, ImGuiViewportPtr vp)
        {
            _gcHandle = GCHandle.Alloc(this);
            

            using (var gameHandle = nodeContext.AppHost.Services.GetGameHandle())
            {
                var registry = gameHandle.Resource.Services;
                var gameContext = GameContextFactory.NewGameContextSDL(640, 480, true);

                _inputSource = InputSourceFactory.NewWindowInputSource(gameContext);
                _gameWindowRenderer = new GameWindowRenderer(registry, gameContext);
                _schedulerSystem = registry.GetService<SchedulerSystem>();

                var manager = _gameWindowRenderer.WindowManager;
                {
                    manager.PreferredBackBufferWidth = 640;
                    manager.PreferredBackBufferHeight = 480;
                    manager.PreferredBackBufferFormat = PixelFormat.R16G16B16A16_Float;
                    manager.PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
                    manager.ShaderProfile = GraphicsProfile.Level_11_0;
                    manager.PreferredGraphicsProfile = [GraphicsProfile.Level_11_0];

                    using (var dev = nodeContext.AppHost.Services.GetDeviceHandle())
                    {
                        manager.PreferredColorSpace = dev.Resource.ColorSpace;
                    }

                    manager.PreferredMultisampleCount = MultisampleCount.None;
                }

                _gameWindowRenderer.Initialize();

                if (_gameWindowRenderer is IContentable contentable)
                {
                    contentable.LoadContent();
                }

                var window = _gameWindowRenderer.Window;
                {
                    window.Position = new Int2(100, 100);
                    window.FullscreenIsBorderlessWindow = true;
                    window.PreferredFullscreenSize = new Int2(-1, -1); // adapt desktop Size
                    window.AllowUserResizing = true;
                    window.IsMouseVisible = true;
                    window.BringToFront();
                }

                _windowRenderer = new WindowRenderer(_gameWindowRenderer);
            }

            vp.PlatformUserData = (IntPtr)_gcHandle;
        }

        public void Update(IGraphicsRendererBase IRenderer)
        {
            _windowRenderer.Input = IRenderer;
            _schedulerSystem.Schedule(_windowRenderer);
        }

        public void Dispose()
        {
            _gameWindowRenderer.Close();
            _inputSource.Dispose();
            _gcHandle.Free();
        }


        class WindowRenderer : IGraphicsRendererBase
        {
            private readonly GameWindowRenderer _gameWindowRenderer;
            private readonly WithRenderTargetAndViewPort _withRenderTargetAndViewPort;

            public IGraphicsRendererBase? Input {  private get; set; }

            public WindowRenderer(GameWindowRenderer gameWindowRenderer)
            {
                _gameWindowRenderer = gameWindowRenderer;
                _withRenderTargetAndViewPort = new WithRenderTargetAndViewPort();
            }

            public void Draw(RenderDrawContext context)
            {
                if (Input != null)
                {
                    var bdraw = _gameWindowRenderer.BeginDraw();
                    if (bdraw)
                    {
                        _withRenderTargetAndViewPort.RenderTarget = _gameWindowRenderer.Presenter.BackBuffer;
                        _withRenderTargetAndViewPort.DepthBuffer = _gameWindowRenderer.Presenter.DepthStencilBuffer;
                        _withRenderTargetAndViewPort.Input = Input;
                        _withRenderTargetAndViewPort.Draw(context);
                    }
                    _gameWindowRenderer.EndDraw();
                }
            }
        }
    }




   
}

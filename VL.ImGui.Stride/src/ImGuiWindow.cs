using ImGuiNET;
using System.Runtime.InteropServices;

using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Stride.Core.Mathematics;
using Stride.Engine;

using VL.Core;
using VL.Stride;
using VL.Stride.Games;
using VL.Stride.Rendering;
using VL.Stride.Engine;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;
using VL.Stride.Input;

namespace VL.ImGui.Stride
{
    using GameWindowRenderer = VL.Stride.Games.GameWindowRenderer;

    internal class ImGuiWindow : IDisposable
    {
        private readonly ImGuiViewportPtr _vp;
        private readonly GCHandle _gcHandle;
        private readonly SchedulerSystem _schedulerSystem;
        private readonly GameContext _gameContext;
        private readonly GameWindowRenderer _gameWindowRenderer;
        private readonly IInputSource _inputSource;
        private readonly WindowRenderer _windowRenderer;
        private readonly ImGuiRenderer _renderer;       

        private object? _state;
        private ImGuiWindowsCreateHandler? _createHandler;
        private ImGuiWindowsDrawHandler? _drawHandler;

        public IntPtr Handle => _gameWindowRenderer.Window.NativeWindow.Handle;

        public event EventHandler<EventArgs> Closing
        {
            add {    if (_gameWindowRenderer.Window != null) _gameWindowRenderer.Window.Closing += value; }
            remove { if (_gameWindowRenderer.Window != null) _gameWindowRenderer.Window.Closing -= value; }
        }

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

        public IInputSource Input
        {
            get { return _inputSource; }
        }

        private bool _isFocused = false;
        public bool IsFocused
        {
            get 
            {
                var focused = _gameWindowRenderer.Window.Focused;

                if (_isFocused != focused)
                {
                    _isFocused = focused;
                    Helper.Log(Title + " " + _gameWindowRenderer.Window.Focused);
                }
                return _isFocused; 
            }
        }

        public void Activate()
        {
            _gameWindowRenderer.Window.BringToFront();
        }

        public bool IsMinimized
        {
            get { return _gameWindowRenderer.Window.IsMinimized; }
        }

        public ImGuiWindow(NodeContext nodeContext, StrideDeviceContext strideDeviceContext, ImGuiViewportPtr vp, Game game)
        {
            _vp = vp;

            Int2 position = new Int2(100, 100);
            Int2 size = new Int2(640, 480);

            if ((_vp.Flags & ImGuiViewportFlags.NoDecoration) != 0)
            {
                position = new Int2((int)_vp.Pos.X, (int)_vp.Pos.Y);
                size = new Int2((int)_vp.Size.X, (int)_vp.Size.Y);
            }


            _gcHandle = GCHandle.Alloc(this);
           
            _gameContext = GameContextFactory.NewGameContextSDL(size.X, size.Y, true);
            _inputSource = InputSourceFactory.NewWindowInputSource(_gameContext);
            //game.Input.Sources.Add(_inputSource);

            _gameWindowRenderer = new GameWindowRenderer(game.Services, _gameContext);
            _schedulerSystem = game.Services.GetService<SchedulerSystem>();

            var manager = _gameWindowRenderer.WindowManager;
            {
                manager.PreferredBackBufferWidth = size.X; 
                manager.PreferredBackBufferHeight = size.Y;
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
                window.Title = "MainImgui";
                window.IsBorderLess = (_vp.Flags & ImGuiViewportFlags.NoDecoration) != 0;
                window.Position = position;
                window.FullscreenIsBorderlessWindow = true;
                window.PreferredFullscreenSize = new Int2(-1, -1); // adapt desktop Size
                window.AllowUserResizing = (_vp.Flags & ImGuiViewportFlags.NoDecoration) == 0; ;
                window.IsMouseVisible = true;
                window.BringToFront();

                window.ClientSizeChanged += Window_ClientSizeChanged;
                window.Closing += Window_Closing; 
                window.Activated += Window_Activated;
                window.Deactivated += Window_Deactivated;
            }

            _windowRenderer = new WindowRenderer(_gameWindowRenderer, _inputSource);

            _renderer = new ImGuiRenderer(strideDeviceContext);

            vp.PlatformUserData = (IntPtr)_gcHandle;
        }

        #region window.Events
        private void Window_Closing(object? sender, EventArgs e)
        {
            _vp.PlatformRequestClose = true;
            Helper.Log("ImGuiWindow.Window_Closing");
        }

        private void Window_ClientSizeChanged(object? sender, EventArgs e)
        {
            _vp.PlatformRequestResize = true;
            Helper.Log("ImGuiWindow.Window_ClientSizeChanged");
        }

        private void Window_Activated(object? sender, EventArgs e)
        {
            Helper.Log("ImGuiWindow.Window_Activated");
        }

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            Helper.Log("ImGuiWindow.Window_Deactivated");
        }
        #endregion window.Events

        public void Update(ImGuiWindowsCreateHandler create, ImGuiWindowsDrawHandler draw, ImDrawDataPtr drawData, Spread<FontConfig?> fonts, IStyle style)
        {
            _createHandler = create;
            _drawHandler = draw;

            if (_state is null && _createHandler != null)
                _createHandler(out _state);

            if (_state != null && _drawHandler != null)
            {
                _renderer.Update(fonts, style);
                _renderer.SetDrawData(drawData);
                _drawHandler(_state, _renderer, _gameWindowRenderer.Window, _gameWindowRenderer.Presenter, out _state, out var output);
                _windowRenderer.Input = output;
                _schedulerSystem.Schedule(_windowRenderer);
            }
        }

        public void Dispose()
        {
            Helper.Log("ImGuiWindow.Dispose()");

            try
            {
                (_state as IDisposable)?.Dispose();
            }
            finally
            {
                _state = new object();
            }

            //_game.Input.Sources.Remove(_inputSource);

            //_inputManager.Sources.Remove(_inputSource);

            _gameWindowRenderer.Window.ClientSizeChanged -= Window_ClientSizeChanged;
            _gameWindowRenderer.Window.Closing -= Window_Closing;
            _gameWindowRenderer.Window.Activated -= Window_Activated;
            _gameWindowRenderer.Window.Deactivated -= Window_Deactivated;
            _gameWindowRenderer.Close();
            //_inputManagerHandle.Dispose();
            _inputSource.Dispose();
            //_game.Dispose();
            //_gameHandle.Dispose();
            _gcHandle.Free();
        }


        class WindowRenderer : IGraphicsRendererBase
        {
            private readonly GameWindowRenderer _gameWindowRenderer;
            private readonly WithRenderTargetAndViewPort _withRenderTargetAndViewPort;

            public IGraphicsRendererBase? Input {  private get; set; }

            public IInputSource? InputSource { get; set; }

            public WindowRenderer(GameWindowRenderer gameWindowRenderer, IInputSource InputSource)
            {
                _gameWindowRenderer = gameWindowRenderer;
                _withRenderTargetAndViewPort = new WithRenderTargetAndViewPort();
                this.InputSource = InputSource;
            }

            public void Draw(RenderDrawContext context)
            {
                if (Input != null)
                {
                    if (_gameWindowRenderer.BeginDraw())
                    {
                        _withRenderTargetAndViewPort.RenderTarget = _gameWindowRenderer.Presenter.BackBuffer;
                        _withRenderTargetAndViewPort.DepthBuffer = _gameWindowRenderer.Presenter.DepthStencilBuffer;
                        _withRenderTargetAndViewPort.Input = Input;

                        context.RenderContext.SetWindowInputSource(InputSource);
                        _withRenderTargetAndViewPort.Draw(context);
                        context.RenderContext.SetWindowInputSource(null);
                    }
                    _gameWindowRenderer.EndDraw();
                }
            }
        }
    }




   
}

using ImGuiNET;
using Stride.Input;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;
using Buffer = Stride.Graphics.Buffer;
using RendererBase = Stride.Rendering.RendererBase;

using VL.Core;
using VL.Lib.Collections;
using VL.Stride;
using VL.Stride.Rendering;
using VL.Stride.Input;

using System.Reactive.Disposables;
using VL.Lib.Basics.Resources;

namespace VL.ImGui
{
    using ImGui = ImGuiNET.ImGui;

    public partial class ImGuiRenderer : RendererBase, IDisposable
    {
        const int INITIAL_VERTEX_BUFFER_SIZE = 128;
        const int INITIAL_INDEX_BUFFER_SIZE = 128;

        // dependencies
        IResourceHandle<GraphicsDevice> deviceHandle;
        IResourceHandle<GraphicsContext> contextHandle;
        IResourceHandle<InputManager> inputHandle;

        GraphicsDevice device => deviceHandle.Resource;
        GraphicsContext context => contextHandle.Resource;
        InputManager input => inputHandle.Resource;
        CommandList commandList;
        

        // ImGui
        private readonly ImGuiIOPtr _io;
        private readonly Context _context;
        private ImDrawDataPtr _drawDataPtr;
        private float _fontScaling;
        private float _uiScaling;
        private bool _readyToBeDrawn;

        private Widget widget;
        private WidgetLabel widgetLabel = new();
        private bool dockingEnabled;
        private Spread<FontConfig> fonts = Spread.Create(FontConfig.Default);
        private bool fullscreenWindow;
        private IStyle style;

        // Stride
        private PipelineState imPipeline;
        private VertexDeclaration imVertLayout;
        private VertexBufferBinding vertexBinding;
        private IndexBufferBinding indexBinding;
        private CustomDrawEffect imShader;
        private Texture fontTexture;
        private Matrix projMatrix;

        private IInputSource lastInputSource;
        private readonly SerialDisposable inputSubscription = new SerialDisposable();
        public unsafe ImGuiRenderer(CustomDrawEffect drawEffect)
        {
            deviceHandle = AppHost.Current.Services.GetDeviceHandle();
            contextHandle = AppHost.Current.Services.GetGraphicsContextHandle();
            inputHandle = AppHost.Current.Services.GetInputManagerHandle();

            imShader = drawEffect;

            _context = new StrideContext();
            using (_context.MakeCurrent())
            {
                _io = ImGui.GetIO();
                _io.NativePtr->IniFilename = null;

                CreateDeviceObjects();

                //var s = ImGui.GetStyle();
                //for (int i = 0; i < s.Colors.Count; i++)
                //{
                //    var color = Unsafe.As<System.Numerics.Vector4, Color4>(ref s.Colors[i]).ToLinear();

                //    var r = (float)Math.Pow(s.Colors[i].X, 2.2);
                //    var g = (float)Math.Pow(s.Colors[i].Y, 2.2);
                //    var b = (float)Math.Pow(s.Colors[i].Z, 2.2);

                //    s.Colors[i] = new System.Numerics.Vector4(r, g, b, s.Colors[i].W);
                //}

                var scaling = VL.UI.Core.DIPHelpers.DIPFactor();
                UpdateScaling(fontScaling: scaling, uiScaling: scaling);
            }
        }
        
        #region scaling
        void UpdateScaling(float fontScaling, float uiScaling)
        {
            if (fontScaling != _fontScaling)
            {
                _fontScaling = fontScaling;
                BuildImFontAtlas(_io.Fonts, fontScaling);
            }
            if (uiScaling != _uiScaling)
            {
                _uiScaling = uiScaling;
                RenderHelper.UpdateUIScaling(uiScaling);
            }
        }
        #endregion scaling




        // need to be called from VL
        public void Update(Widget? widget, bool dockingEnabled, Spread<FontConfig> fonts, bool fullscreenWindow, IStyle style)
        {
            this.widget = widget;
            this.dockingEnabled = dockingEnabled;

            if (!fonts.IsEmpty && !this.fonts.SequenceEqual(fonts))
            {
                this.fonts = fonts;
                BuildImFontAtlas(_io.Fonts, _fontScaling);
            }

            this.fullscreenWindow = fullscreenWindow;
            this.style = style;
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var commandList = context.CommandList;
            var renderTarget = commandList.RenderTarget;

            using (_context.MakeCurrent())
            {
                _io.DisplaySize = new System.Numerics.Vector2(renderTarget.Width, renderTarget.Height);
                _io.DisplayFramebufferScale = new System.Numerics.Vector2(1.0f, 1.0f);
                _io.DeltaTime = (float)context.RenderContext.Time.TimePerFrame.TotalSeconds;

                projMatrix = Matrix.OrthoRH(renderTarget.Width, -renderTarget.Height, -1, 1);

                var inputSource = context.RenderContext.GetWindowInputSource();
                if (inputSource != lastInputSource)
                {
                    lastInputSource = inputSource;
                    inputSubscription.Disposable = SubscribeToInputSource(inputSource, context);
                }

                // Enable Docking
                if (dockingEnabled)
                    _io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

                _context.NewFrame();

                try
                {
                    using var _ = _context.ApplyStyle(style);

                    if (fullscreenWindow)
                    {
                        var viewPort = ImGui.GetMainViewport();
                        ImGui.SetNextWindowPos(viewPort.WorkPos);
                        ImGui.SetNextWindowSize(viewPort.WorkSize);
                        ImGui.Begin(widgetLabel.Update(null),
                            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                            ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
                            ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoDecoration |
                            ImGuiWindowFlags.NoBackground);
                    }

                    // Enable Docking
                    if (dockingEnabled)
                    {
                        ImGui.DockSpaceOverViewport();
                    }

                    _context.SetDrawList(DrawList.Foreground);
                    _context.Update(widget);
                }
                finally
                {
                    if (dockingEnabled)
                    {
                        ImGui.End();
                    }

                    if (fullscreenWindow)
                    {
                        ImGui.End();
                    }

                    // Render (builds mesh with texture coordinates)
                    ImGui.Render();
                }

                // Render the mesh
                _drawDataPtr = ImGui.GetDrawData();
                _readyToBeDrawn = true;
            }

            imShader.SetParameters(context.RenderContext.RenderView, context);
            RenderDrawLists(_drawDataPtr);
        }

        protected override void Destroy()
        {
            imPipeline.Dispose();
            vertexBinding.Buffer.Dispose();
            indexBinding.Buffer.Dispose();
            fontTexture.Dispose();
            imShader.Dispose();
            inputSubscription.Dispose();

            deviceHandle.Dispose();
            contextHandle.Dispose();
            inputHandle.Dispose();

            base.Destroy();
        }
    }
}

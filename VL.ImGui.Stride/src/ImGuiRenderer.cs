using ImGuiNET;
using Stride.Input;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Core.Mathematics;
using Buffer = Stride.Graphics.Buffer;
using RendererBase = Stride.Rendering.RendererBase;

using VL.Core;
using VL.Lib.Collections;
using VL.Stride;
using VL.Stride.Input;

using System.Reactive.Disposables;
using VL.Lib.Basics.Resources;
using VL.ImGui.Stride.Effects;
using Stride.Shaders.Compiler;


namespace VL.ImGui
{
    using ImGui = ImGuiNET.ImGui;

    public partial class ImGuiRenderer : RendererBase, IDisposable
    {
        private readonly StrideDeviceContext _strideDeviceContext;

        private ImDrawDataPtr _drawData;

        // VL.ImGui
        private Widget? widget;
        private WidgetLabel widgetLabel = new();
        private bool dockingEnabled;
        private bool fullscreenWindow;
        private IStyle? style;

        // Stride.Input
        private IInputSource? lastInputSource;
        private readonly SerialDisposable inputSubscription = new SerialDisposable();

        internal unsafe ImGuiRenderer(StrideDeviceContext strideDeviceContext)
        {
            _strideDeviceContext = strideDeviceContext;
        }

        public unsafe ImGuiRenderer(NodeContext nodeContext)
        {
            _strideDeviceContext = new StrideDeviceContext(nodeContext);
        }
        
        internal void SetDrawData(ImDrawDataPtr _drawData)
        {
            this._drawData = _drawData;
        }

        internal void Update( Spread<FontConfig?> fonts, IStyle style)
        {
            _strideDeviceContext.SetFonts(fonts);
            this.style = style;
        }

        // need to be called from VL
        public void Update(Widget? widget, bool dockingEnabled, Spread<FontConfig?> fonts, bool fullscreenWindow, IStyle style)
        {
            this.widget = widget;
            this.dockingEnabled = dockingEnabled;
            this.fullscreenWindow = fullscreenWindow;
            Update(fonts, style);
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var commandList = context.CommandList;
            var renderTarget = commandList.RenderTarget;

            using (_strideDeviceContext.MakeCurrent())
            {
                _strideDeviceContext.IO.DeltaTime = (float)context.RenderContext.Time.TimePerFrame.TotalSeconds;

                var inputSource = context.RenderContext.GetWindowInputSource();
                if (inputSource != lastInputSource)
                {
                    lastInputSource = inputSource;
                    inputSubscription.Disposable = SubscribeToInputSource(inputSource, context);
                }
                
                if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) == 0)
                {
                    _strideDeviceContext.IO.DisplaySize = new System.Numerics.Vector2(renderTarget.Width, renderTarget.Height);
                    _strideDeviceContext.IO.DisplayFramebufferScale = new System.Numerics.Vector2(1.0f, 1.0f);

                    // Enable Docking
                    if (dockingEnabled)
                        _strideDeviceContext.IO.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

                    _strideDeviceContext.NewFrame();

                    try
                    {
                        using var _ = _strideDeviceContext.ApplyStyle(style);

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

                        _strideDeviceContext.SetDrawList(DrawList.AtCursor);
                        _strideDeviceContext.Update(widget);
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

                    SetDrawData(ImGui.GetDrawData());              
                }
            }
            _strideDeviceContext.RenderDrawLists(context, _drawData);

        }

        private readonly CompositeDisposable errorImGuiInsideImGui = new CompositeDisposable();
        internal void ErrorImGuiInsideImGui()
        {
            if (IVLRuntime.Current != null)
            {
                if (errorImGuiInsideImGui.Count == 0)
                {
                    foreach (var id in _strideDeviceContext.path)
                    {
                        errorImGuiInsideImGui.Add(IVLRuntime.Current.AddPersistentMessage(new Lang.Message(id, Lang.MessageSeverity.Error, "Don't use ImGui[Renderer] inside ImGui[Renderer]")));
                    }
                } 
            }   
        }

        protected override void Destroy()
        {
            errorImGuiInsideImGui.Dispose(); ;
            inputSubscription.Dispose();
            _strideDeviceContext.Dispose();

            base.Destroy();
        }
    }
}

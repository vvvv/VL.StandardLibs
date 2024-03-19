using Stride.Input;
using Stride.Rendering;
using Stride.Graphics;
using Stride.Core.Mathematics;

namespace VL.ImGui
{
    public interface IWithParentInputSourceAndViewport
    {
        public IInputSource? ParentInputSource { get;  set; }
        public Viewport? Viewport { get; set; }
    }

    internal sealed class RenderLayerWithInputSource : IWithParentInputSourceAndViewport, IDisposable
    {
        private readonly InputManager inputManager;

        // Will set from RenderWidget
        public bool HasFocus { get; internal set; }
        public IGraphicsRendererBase? Layer { get; internal set; }
        public RenderView? RenderView { get; internal set; }
        public Viewport? Viewport { get; set; }

        // Will Set from ImGuiRenderer
        public IInputSource? ParentInputSource
        {
            get
            {
                return null;
            }
            set
            {
                if (value == null)
                {
                    MappedInputSource?.Dispose();
                }
                else
                {
                    MappedInputSource = new MappedInputSource(inputManager, value);
                }

            }
        }

        // Mapped Inputsource
        public IInputSource? MappedInputSource { get; private set; }

        public RenderLayerWithInputSource(InputManager inputManager)
        {
            this.inputManager = inputManager;
        }

        public void Dispose()
        {
            MappedInputSource?.Dispose();
        }
    }

    //internal sealed class RenderLayerWithInputSource : IWithParentInputSourceAndViewport, IDisposable
    //{

    //    // Will set from RenderWidget
    //    public bool HasFocus { get; internal set; }
    //    public IGraphicsRendererBase? Layer { get; internal set; }
    //    public RenderView? RenderView { get; internal set; }
    //    public Viewport? Viewport { get; set; }

    //    // Will Set from ImGuiRenderer
    //    public IInputSource? ParentInputSource { get; set; }
    //    public Vector2 ParentSurfaceSize { get; set; }

    //    // Mapped Inputsource
    //    public IInputSource MappedInputSource { get; set; }

    //    public RenderLayerWithInputSource(InputManager inputManager)
    //    {
    //        MappedInputSource = new ImputSourceViewPort(this, inputManager);
    //    }

    //    public void Dispose()
    //    {
    //        MappedInputSource.Dispose();
    //    }
    //}
}

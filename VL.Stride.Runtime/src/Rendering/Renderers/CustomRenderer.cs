using Stride.Rendering;
using Stride.Rendering.Compositing;
using System;
using VL.Core;

namespace VL.Stride.Rendering
{
    /// <summary>
    /// A renderer which can provide <see cref="RendererBase.Draw"/> implementation with a stateful region.
    /// </summary>
    public sealed class CustomRenderer<TState> : SceneRendererBase
        where TState : class
    {
        private Func<TState> CreateFunc;
        private Func<TState, RenderDrawContext, TState> UpdateFunc;
        private TState State;

        public void Update(Func<TState> create, Func<TState, RenderDrawContext, TState> update)
        {
            CreateFunc = create;
            UpdateFunc = update;
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            var state = State ?? CreateFunc?.Invoke();
            State = UpdateFunc?.Invoke(state, drawContext);
        }

        protected override void Destroy()
        {
            if (State is IDisposable disposable)
                disposable.Dispose();

            base.Destroy();
        }
    }

    //public sealed class ProxyRenderer<TState> : ISceneRenderer
    //{
    //    public ISceneRenderer Input { get; private set; }

    //    public ProxyRenderer(ISceneRenderer input)
    //    {
    //        Input = input;
    //    }

    //    public bool Enabled
    //    {
    //        get => Input?.Enabled ?? false;
    //        set
    //        {
    //            if (Input != null)
    //                Input.Enabled = value;
    //        }
    //    }

    //    public bool Initialized => Input?.Initialized ?? false;

    //    public void Collect(RenderContext context)
    //    {
    //        Input?.Collect(context);
    //    }

    //    public void Dispose()
    //    {
    //        Input?.Dispose();
    //    }

    //    public void Draw(RenderDrawContext context)
    //    {
    //        Input?.Draw(context);
    //    }

    //    public void Initialize(RenderContext context)
    //    {
    //        Input?.Initialize(context);
    //    }
    //}
}

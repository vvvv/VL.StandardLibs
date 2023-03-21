using SkiaSharp;
using Stride.Core.Mathematics;
using System;
using System.Threading;
using VL.Core;
using VL.Lib.IO.Notifications;

namespace VL.Skia
{
    public class SetRender : LinkedLayerBase
    {
        Action<ILayer, CallerInfo> OnRender;
        RectangleF? bounds;

        public void Update(ILayer input, Action<ILayer, CallerInfo> render, RectangleF? bounds, out ILayer output)
        {
            Input = input;
            OnRender = render;
            this.bounds = bounds;
            output = this;
        }

        public override void Render(CallerInfo caller)
        {
            OnRender?.Invoke(Input, caller);
        }

        public override RectangleF? Bounds => bounds;
    }

    public class SetNotify : LinkedLayerBase
    {
        Func<INotification, CallerInfo, bool> OnNotify;

        public void Update(ILayer input, Func<INotification, CallerInfo, bool> notify, out ILayer output)
        {
            Input = input;
            OnNotify = notify;
            output = this;
        }

        public override bool Notify(INotification notification, CallerInfo caller)
        {
            return OnNotify?.Invoke(notification, caller) ?? false;
        }
    }

    public class SetBehavior : LinkedLayerBase
    {
        IBehavior Behavior;

        public void Update(ILayer input, IBehavior behavior, out ILayer output)
        {
            Input = input;
            Behavior = behavior;
            output = this;
        }

        public override bool Notify(INotification notification, CallerInfo caller)
        {
            var processed = Input != null
                ? Input.Notify(notification, caller)
                : false;
             
            if (!processed)
                return Behavior?.Notify(notification, caller) ?? false;
            return false;
        }
    }

    public class SetRendering : LinkedLayerBase
    {
        IRendering Rendering;

        public void Update(ILayer input, IRendering rendering, out ILayer output)
        {
            Input = input;
            Rendering = rendering;
            output = this;
        }

        public override void Render(CallerInfo caller)
        {
            Rendering?.Render(caller);
            base.Render(caller);
        }

        public override RectangleF? Bounds => Rendering?.Bounds;
    }

    public class HackRendering<TState, TPaint> : LinkedLayerBase, IDisposable where TPaint: class
    {
        TState FState;
        Func<TState, TPaint, Tuple<TState, TPaint>> RenderPaintHack;

        public void Update(Func<TState> create, Func<TState, TPaint, Tuple<TState, TPaint>> renderPaintHack, ILayer input, out ILayer output, bool Enabled = true)
        {
            if (FState == null)
                FState = create();

            if (Enabled)
            {
                Input = input;
                RenderPaintHack = renderPaintHack;

                output = this;
            }
            else
            {
                output = input;
            }
        }

        public override void Render(CallerInfo caller)
        {
            try
            {
                caller = caller.WithRenderPaintHack(paint =>
                {
                    var tuple = RenderPaintHack?.Invoke(FState, paint as TPaint);
                    FState = tuple.Item1;
                    return tuple.Item2;
                });
            }
            catch (Exception exception)
            {
                RuntimeGraph.ReportException(exception);
            }
            base.Render(caller);
        }

        public void Dispose()
        {
            DisposeInternalState();
        }
        private void DisposeInternalState()
        {
            try
            {
                (FState as IDisposable)?.Dispose();
            }
            finally
            {
                FState = default(TState);
            }
        }
    }

    public class TweakCallerInfo : LinkedLayerBase
    {
        public Func<CallerInfo, CallerInfo> Tweaker;

        public override void Render(CallerInfo caller)
        {
            if (Tweaker != null)
                caller = Tweaker?.Invoke(caller);
            base.Render(caller);
        }

        public override bool Notify(INotification notification, CallerInfo caller)
        {
            if (Tweaker != null)
                caller = Tweaker?.Invoke(caller);
            return base.Notify(notification, caller);
        }

        public void Update(ILayer input, Func<CallerInfo, CallerInfo> tweaker, out ILayer output)
        {
            Input = input;
            Tweaker = tweaker;
            output = this;
        }

    }

}

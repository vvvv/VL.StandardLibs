using System;
using System.Linq;
using System.Reactive.Disposables;
using Microsoft.Extensions.Logging;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.Images;
using VL.Core;

namespace VL.Stride.Rendering
{
    public class TextureFXEffect : ImageEffectShader
    {
        private TimeSpan? lastExceptionTime;
        private TimeSpan retryTime = TimeSpan.FromSeconds(3);

        PerFrameParameters[] perFrameParams;
        PerViewParameters[] perViewParams;

        private readonly ILogger logger;

        public TextureFXEffect(string effectName = null, bool delaySetRenderTargets = false, ILogger logger = null)
            : base(effectName, delaySetRenderTargets)
        {
            this.logger = logger ?? AppHost.Global.DefaultLogger;
            Subscriptions.DisposeBy(this);
        }

        internal readonly CompositeDisposable Subscriptions = new CompositeDisposable();

        protected override void InitializeCore()
        {
            base.InitializeCore();

            EffectInstance.UpdateEffect(GraphicsDevice);
            perFrameParams = EffectInstance.Parameters.GetWellKnownParameters(WellKnownParameters.PerFrameMap).ToArray();
            perViewParams = EffectInstance.Parameters.GetWellKnownParameters(WellKnownParameters.PerViewMap).ToArray();
        }

        public bool IsOutputAssigned => OutputCount > 0 && GetOutput(0) != null;

        protected override void PreDrawCore(RenderDrawContext context)
        {
            if (IsOutputAssigned)
            {
                base.PreDrawCore(context);
                Parameters.SetPerFrameParameters(perFrameParams, context.RenderContext);
            }
        }

        int lastViewWidth;
        int lastViewHeight;
        protected override void UpdateParameters()
        {
            base.UpdateParameters();

            var output0 = GetOutput(0); //safe because it will only be called from base.PreDrawCore when IsOutputAssigned = true
            var w = output0.ViewWidth;
            var h = output0.ViewHeight;

            if (w != lastViewWidth || h != lastViewHeight) //rarely changes
            {
                var viewSize = new Vector2(w, h);
                Parameters.SetCameraParametersOnly(perViewParams, ref viewSize);
                lastViewWidth = w;
                lastViewHeight = h;
            }
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var time = context.RenderContext.Time;
            if (time != null && lastExceptionTime.HasValue && (time.Total - lastExceptionTime) < retryTime)
                return;

            if (IsOutputAssigned)
            {
                try
                {
                    base.DrawCore(context);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Drawing {Name} crashed.", Name);
                    if (time != null)
                        lastExceptionTime = time.Total;
                }
            }
        }

        protected override void PostDrawCore(RenderDrawContext context)
        {
            if (IsOutputAssigned)
                base.PostDrawCore(context);
        }
    }
}

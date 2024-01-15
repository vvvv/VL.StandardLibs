using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Shaders;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;

namespace VL.Stride.Rendering
{
    public class CustomDrawEffect : IEffect, IDisposable
    {
        public readonly EffectInstance EffectInstance;
        readonly PerFrameParameters[] perFrameParams;
        readonly PerViewParameters[] perViewParams;
        readonly PerDrawParameters[] perDrawParams;
        readonly TexturingParameters[] texturingParams;

        public CustomDrawEffect(EffectInstance effectinstance, GraphicsDevice graphicsDevice)
        {
            EffectInstance = effectinstance;

            perFrameParams = EffectInstance.Parameters.GetWellKnownParameters(WellKnownParameters.PerFrameMap).ToArray();
            perViewParams = EffectInstance.Parameters.GetWellKnownParameters(WellKnownParameters.PerViewMap).ToArray();
            perDrawParams = EffectInstance.Parameters.GetWellKnownParameters(WellKnownParameters.PerDrawMap).ToArray();
            texturingParams = EffectInstance.Parameters.GetTexturingParameters().ToArray();
        }

        public ParameterCollection Parameters => EffectInstance.Parameters;

        internal readonly CompositeDisposable Subscriptions = new CompositeDisposable();

        public Action<ParameterCollection, RenderView, RenderDrawContext> ParameterSetter { get; set; }

        public IVLPin<Matrix> WorldIn { get; set; }

        public void Dispose()
        {
            Subscriptions.Dispose();
            EffectInstance.Dispose();
        }

        public EffectInstance SetParameters(RenderView renderView, RenderDrawContext renderDrawContext)
        {
            EffectInstance.UpdateEffect(renderDrawContext.GraphicsDevice);

            var parameters = EffectInstance.Parameters;
            try
            {
                // TODO1: PerFrame could be done in Update if we'd have access to frame time
                // TODO2: This code can be optimized by using parameter accessors and not parameter keys
                parameters.SetPerFrameParameters(perFrameParams, renderDrawContext.RenderContext);

                var parentTransformation = renderDrawContext.RenderContext.Tags.Get(EntityRendererRenderFeature.CurrentParentTransformation);
                if (WorldIn != null)
                {
                    var world = WorldIn.Value;
                    Matrix.Multiply(ref world, ref parentTransformation, out var result);
                    parameters.SetPerDrawParameters(perDrawParams, renderView, ref result);
                }
                else
                {
                    parameters.SetPerDrawParameters(perDrawParams, renderView, ref parentTransformation);
                }

                parameters.SetPerViewParameters(perViewParams, renderView);

                parameters.SetTexturingParameters(texturingParams);

                ParameterSetter?.Invoke(parameters, renderView, renderDrawContext);
            }
            catch (Exception e)
            {
                RuntimeGraph.ReportException(e);
            }
            return EffectInstance;
        }
    }
}

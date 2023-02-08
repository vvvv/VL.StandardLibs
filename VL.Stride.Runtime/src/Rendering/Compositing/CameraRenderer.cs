using Stride.Rendering;
using Stride.Rendering.Compositing;
using System;
using VL.Core;
using VL.Lib.Experimental;
using VL.Stride.Input;

namespace VL.Stride.Rendering.Compositing
{
    // currently not in use
    class CameraRenderer : SceneExternalCameraRenderer, IDisposable
    {
        Sender<object, object> S;

        public CameraRenderer(NodeContext nodeContext)
        {
            S = new Sender<object, object>(nodeContext, this, null);
        }

        protected override void PreDrawCore(RenderDrawContext renderDrawContext)
        {
            var renderContext = renderDrawContext.RenderContext;
            var inputSouce = renderContext.GetWindowInputSource();
            S.Value = inputSouce;
            base.PreDrawCore(renderDrawContext);
        }

        void IDisposable.Dispose()
        {
            base.Dispose();
            S.Dispose();
        }
    }
}

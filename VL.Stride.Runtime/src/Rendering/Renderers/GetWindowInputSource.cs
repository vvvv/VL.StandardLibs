using Stride.Input;
using Stride.Rendering;
using System;
using System.Diagnostics;
using VL.Stride.Input;

namespace VL.Stride.Rendering
{
    public class GetWindowInputSource : RendererBase
    {
        public IInputSource InputSource { get; private set; }

        public override bool AlwaysRender => true;

        protected override void DrawInternal(RenderDrawContext context)
        {
            var renderContext = context.RenderContext;
            var inputSource = renderContext.GetWindowInputSource();
            InputSource = inputSource;

            Input?.Draw(context);
        }
    }
}

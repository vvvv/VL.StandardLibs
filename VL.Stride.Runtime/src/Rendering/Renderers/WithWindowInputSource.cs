using Stride.Core;
using Stride.Input;
using Stride.Rendering;
using VL.Stride.Input;

namespace VL.Stride.Rendering
{
    public class WithWindowInputSource : RendererBase
    {
        public IInputSource InputSource { get; set; }

        protected override void DrawInternal(RenderDrawContext context)
        {
            var inputSource = InputSource;
            using (context.RenderContext.PushTagAndRestore(InputExtensions.WindowInputSource, inputSource))
            {
                DrawInput(context);
            }
        }
    }
}

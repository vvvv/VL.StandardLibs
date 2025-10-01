using Microsoft.Extensions.Logging;
using Stride.Rendering;
using System;
using System.Diagnostics;
using VL.Core;

namespace VL.Stride.Rendering
{
    public abstract class RendererBase : IGraphicsRendererBase
    {
        private readonly ILogger _logger;

        public RendererBase()
        {
            _logger = AppHost.Current.LoggerFactory.CreateLogger(GetType());
        }

        public IGraphicsRendererBase Input { get; set; }

        public virtual bool AlwaysRender { get; }

        public void Draw(RenderDrawContext context)
        {
            if (AlwaysRender || Input != null)
            {
                try
                {
                    DrawInternal(context);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unexpected exception while drawing");
                }
            }
        }

        /// <summary>
        /// Gets called if the input is assigned or <see cref="AlwaysRender"/> is true.
        /// </summary>
        /// <param name="context">The context.</param>
        protected abstract void DrawInternal(RenderDrawContext context);

        protected void DrawInput(RenderDrawContext context)
        {
            Input.Draw(context);
        }
    }
}

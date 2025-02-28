using Stride.Core;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using System.Linq;
using System.Reflection;
using VL.Stride.Input;

namespace VL.Stride.Engine
{
    /// <summary>
    /// Renders a scene instance with a graphics compositor.
    /// </summary>
    public class SceneInstanceRenderer : RendererBase
    {
        /// <summary>
        /// The fallback scene to use in case no scene is connected. 
        /// This is needed because we clear the render target through the compositor which in turn expects visibility groups only provided by the scene instance.
        /// </summary>
        private SceneInstance fallbackSceneInstance;

        /// <summary>
        /// Gets or sets the scene instance.
        /// </summary>
        public SceneInstance SceneInstance { get; set; }

        /// <summary>
        /// Gets or sets the graphics compositor.
        /// </summary>
        public GraphicsCompositor GraphicsCompositor { get; set; }

        protected override void InitializeCore()
        {
            base.InitializeCore();
            fallbackSceneInstance = new SceneInstance(Services);
        }

        protected override void DrawCore(RenderDrawContext renderDrawContext)
        {
            // Game runs after Patch.Update which in turn could've disposed the system
            if (IsDisposed)
                return;

            var renderContext = renderDrawContext.RenderContext;
            var sceneInstance = SceneInstance ?? fallbackSceneInstance;

            // Execute Draw step of SceneInstance
            // This will run entity processors
            sceneInstance.Draw(renderContext);

            renderDrawContext.ResourceGroupAllocator.Flush();
            renderDrawContext.QueryManager.Flush();

            // The graphics compositor is also clearing the render target
            using (renderDrawContext.RenderContext.PushTagAndRestore(SceneInstance.Current, sceneInstance))
            {
                GraphicsCompositor?.Draw(renderDrawContext);
            }
        }
    }
}

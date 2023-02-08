using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using VL.Core;

namespace VL.Stride.Engine
{
    /// <summary>
    /// Allows to schedule game systems (e.g. a SceneSystem or a LayerSystem) as well as single renderers.
    /// </summary>
    public class SchedulerSystem : GameSystemBase
    {
        static readonly PropertyKey<bool> contentLoaded = new PropertyKey<bool>("ContentLoaded", typeof(SchedulerSystem));

        readonly List<GameSystemBase> queue = new List<GameSystemBase>();
        readonly Stack<ConsecutiveRenderSystem> pool = new Stack<ConsecutiveRenderSystem>();

        public SchedulerSystem([NotNull] IServiceRegistry registry) : base(registry)
        {
            Enabled = true;
            Visible = true;
        }

        /// <summary>
        /// Schedule a game system to be processed in this frame.
        /// </summary>
        /// <param name="gameSystem">The game system to schedule.</param>
        public void Schedule(GameSystemBase gameSystem)
        {
            queue.Add(gameSystem);
        }

        /// <summary>
        /// Schedules a renderer for rendering.
        /// </summary>
        /// <param name="renderer">The layer to schedule.</param>
        public void Schedule(IGraphicsRendererBase renderer)
        {
            var current = queue.LastOrDefault() as ConsecutiveRenderSystem;
            if (current is null)
            {
                // Fetch from pool or create new
                current = pool.Count > 0 ? pool.Pop() : new ConsecutiveRenderSystem(Services);
                Schedule(current);
            }
            current.Renderers.Add(renderer);
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var system in queue)
            {
                if (!system.Tags.Get(contentLoaded) && system is IContentable c)
                {
                    c.LoadContent();
                    system.Tags.Set(contentLoaded, true);
                }
                if (system.Enabled)
                    system.Update(gameTime);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            var renderContext = RenderContext.GetShared(Services);

            // Reset the context
            renderContext.Reset();

            // Recycle temporary resources (for example textures allocated by render features through GetTemporaryTexture)
            renderContext.Allocator.Recycle(r => r.AccessCountSinceLastRecycle == 0);

            try
            {
                foreach (var system in queue)
                {
                    if (system.Visible)
                    {
                        if (system.BeginDraw())
                        {
                            system.Draw(gameTime);
                            system.EndDraw();
                        }
                    }
                }
            }
            finally
            {
                // Put back into the pool
                foreach (var system in queue)
                    if (system is ConsecutiveRenderSystem c)
                        pool.Push(c);

                queue.Clear();
            }
        }

        sealed class ConsecutiveRenderSystem : GameSystemBase
        {
            private RenderView renderView;
            private RenderContext renderContext;
            private RenderDrawContext renderDrawContext;

            public ConsecutiveRenderSystem([NotNull] IServiceRegistry registry) : base(registry)
            {
                Enabled = true;
                Visible = true;
            }

            public readonly List<IGraphicsRendererBase> Renderers = new List<IGraphicsRendererBase>();

            protected override void LoadContent()
            {
                // Default render view
                renderView = new RenderView()
                {
                    NearClipPlane = 0.05f,
                    FarClipPlane = 1000,
                };

                // Create the drawing context
                var graphicsContext = Services.GetSafeServiceAs<GraphicsContext>();
                renderContext = RenderContext.GetShared(Services);
                renderDrawContext = new RenderDrawContext(Services, renderContext, graphicsContext);
            }

            public override void Draw(GameTime gameTime)
            {
                try
                {
                    using (renderContext.PushRenderViewAndRestore(renderView))
                    using (renderDrawContext.PushRenderTargetsAndRestore())
                    {
                        // Report the exceptions but continue drawing the next renderer. Otherwise one failing renderer can cause the whole app to fail.
                        foreach (var renderer in Renderers)
                        {
                            try
                            {
                                renderer?.Draw(renderDrawContext);
                            }
                            catch (Exception e)
                            {
                                RuntimeGraph.ReportException(e);
                            }
                        }
                    }
                }
                finally
                {
                    Renderers.Clear();
                    renderDrawContext.ResourceGroupAllocator.Flush();
                    renderDrawContext.QueryManager.Flush();
                }
            }
        }
    }
}

#nullable enable
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VL.Core;

namespace VL.Stride.Engine
{
    /// <summary>
    /// Allows to schedule game systems (e.g. a SceneSystem or a LayerSystem) as well as single renderers.
    /// </summary>
    public class SchedulerSystem : GameSystemBase
    {
        internal readonly ref struct CustomScheduler(SchedulerSystem schedulerSystem, Action<IGraphicsRendererBase>? previous)
        {
            public void Dispose()
            {
                schedulerSystem.privateScheduler = previous;
            }
        }

        static readonly PropertyKey<bool> contentLoaded = new PropertyKey<bool>("ContentLoaded", typeof(SchedulerSystem));

        private List<GameSystemBase> front = new List<GameSystemBase>();
        private List<GameSystemBase> back = new List<GameSystemBase>();
        private readonly Stack<ConsecutiveRenderSystem> pool = new Stack<ConsecutiveRenderSystem>();
        private Action<IGraphicsRendererBase>? privateScheduler;

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
            front.Add(gameSystem);
        }

        /// <summary>
        /// Schedules a renderer for rendering.
        /// </summary>
        /// <param name="renderer">The layer to schedule.</param>
        public void Schedule(IGraphicsRendererBase renderer)
        {
            if (privateScheduler != null)
            {
                privateScheduler.Invoke(renderer);
                return;
            }

            var current = front.LastOrDefault() as ConsecutiveRenderSystem;
            if (current is null)
            {
                // Fetch from pool or create new
                current = pool.Count > 0 ? pool.Pop() : new ConsecutiveRenderSystem(Services);
                Schedule(current);
            }
            current.Renderers.Add(renderer);
        }

        /// <summary>
        /// Removes a renderer from the scheduler. Used by RendererScheduler node on Dispose to ensure that the renderer is not called after it has been disposed.
        /// </summary>
        public void Remove(IGraphicsRendererBase? renderer)
        {
            if (renderer is null)
                return;

            foreach (var s in front)
                if (s is ConsecutiveRenderSystem crs)
                    crs.Renderers.Remove(renderer);
        }

        /// <summary>
        /// Allows to install a custom schedulerer. Used by regions like CustomPostFX during their draw call.
        /// </summary>
        internal CustomScheduler WithPrivateScheduler(Action<IGraphicsRendererBase> scheduler)
        {
            var previous = Interlocked.Exchange(ref privateScheduler, scheduler);
            return new CustomScheduler(this, previous);
        }

        public override void Update(GameTime gameTime)
        {
            // Game runs after Patch.Update which in turn could've disposed the system
            if (IsDisposed)
                return;

            foreach (var system in front)
            {
                if (system.IsDisposed)
                    continue;

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
            // Game runs after Patch.Update which in turn could've disposed the system
            if (IsDisposed) 
                return;

            var renderContext = RenderContext.GetShared(Services);

            // Reset the context
            renderContext.Reset();

            // Recycle temporary resources (for example textures allocated by render features through GetTemporaryTexture)
            renderContext.Allocator.Recycle(r => r.AccessCountSinceLastRecycle == 0);

            var queue = front;
            Utilities.Swap(ref front, ref back);
            try
            {
                foreach (var system in queue)
                {
                    if (system.IsDisposed)
                        continue;

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
            private List<IGraphicsRendererBase> front = new();
            private List<IGraphicsRendererBase> back = new();

            private RenderView? renderView;
            private RenderContext? renderContext;
            private RenderDrawContext? renderDrawContext;

            public ConsecutiveRenderSystem([NotNull] IServiceRegistry registry) : base(registry)
            {
                Enabled = true;
                Visible = true;
            }

            public List<IGraphicsRendererBase> Renderers => front;

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
                var renderers = front;
                Utilities.Swap(ref front, ref back);
                try
                {
                    using (renderContext!.PushRenderViewAndRestore(renderView))
                    using (renderDrawContext!.PushRenderTargetsAndRestore())
                    {
                        // Report the exceptions but continue drawing the next renderer. Otherwise one failing renderer can cause the whole app to fail.
                        foreach (var renderer in renderers)
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
                    renderers.Clear();
                    renderDrawContext!.ResourceGroupAllocator.Flush();
                    renderDrawContext!.QueryManager.Flush();
                }
            }
        }
    }
}

using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using System;
using System.Collections.Generic;
using System.IO;
using VL.Core;
using VL.Stride.Engine;
using VL.Stride.Rendering;

namespace VL.Stride.Games
{
    public class VLGame : Game
    {
        private readonly TimeSpan maximumElapsedTime = TimeSpan.FromMilliseconds(2000.0);
        private TimeSpan accumulatedElapsedGameTime;
        private bool forceElapsedTimeToZero;

        internal readonly SchedulerSystem SchedulerSystem;
        private NodeFactoryRegistry NodeFactoryRegistry;

        public VLGame()
            : base()
        {
            SchedulerSystem = new SchedulerSystem(Services);
            Services.AddService(SchedulerSystem);
        }

        protected override void Destroy()
        {
            base.Destroy();
        }

        public TimeSpan ElapsedUserTime;

        // Used to post-pone the present calls to the very end of a frame
        internal readonly List<GameWindowRenderer> PendingPresentCalls = new List<GameWindowRenderer>();

        protected override void PrepareContext()
        {
            base.PrepareContext();
        }

        protected override void OnWindowCreated()
        {
            Window.AllowUserResizing = true;
            base.OnWindowCreated();
        }

        protected override void Initialize()
        {
            Settings.EffectCompilation = EffectCompilationMode.Local;
            Settings.RecordUsedEffects = false;

            base.Initialize();

            GameSystems.Add(SchedulerSystem);

            // Setup the effect compiler with our file provider so we can attach shader lookup paths per document
            EffectSystem.InstallEffectCompilerWithCustomPaths();
        }

        /// <summary>
        /// As per https://github.com/stride3d/stride/pull/497 this is the entry point to modify the update logic.
        /// This is the same code as in the base class except that the elapsed time is given by our clock.
        /// </summary>
        protected override void RawTickProducer()
        {
            try
            {
                var elapsedAdjustedTime = ElapsedUserTime;

                if (forceElapsedTimeToZero)
                {
                    elapsedAdjustedTime = TimeSpan.Zero;
                    forceElapsedTimeToZero = false;
                }

                if (elapsedAdjustedTime > maximumElapsedTime)
                {
                    elapsedAdjustedTime = maximumElapsedTime;
                }

                bool drawFrame = true;
                int updateCount = 1;
                var singleFrameElapsedTime = elapsedAdjustedTime;
                var drawLag = 0L;

                if (/*suppressDraw || */Window.IsMinimized && DrawWhileMinimized == false)
                {
                    drawFrame = false;
                    //suppressDraw = false;
                }

                if (IsFixedTimeStep)
                {
                    // If the rounded TargetElapsedTime is equivalent to current ElapsedAdjustedTime
                    // then make ElapsedAdjustedTime = TargetElapsedTime. We take the same internal rules as XNA
                    if (Math.Abs(elapsedAdjustedTime.Ticks - TargetElapsedTime.Ticks) < (TargetElapsedTime.Ticks >> 6))
                    {
                        elapsedAdjustedTime = TargetElapsedTime;
                    }

                    // Update the accumulated time
                    accumulatedElapsedGameTime += elapsedAdjustedTime;

                    // Calculate the number of update to issue
                    if (ForceOneUpdatePerDraw)
                    {
                        updateCount = 1;
                    }
                    else
                    {
                        updateCount = (int)(accumulatedElapsedGameTime.Ticks / TargetElapsedTime.Ticks);
                    }

                    if (IsDrawDesynchronized)
                    {
                        drawLag = accumulatedElapsedGameTime.Ticks % TargetElapsedTime.Ticks;
                    }
                    else if (updateCount == 0)
                    {
                        drawFrame = false;
                        // If there is no need for update, then exit
                        return;
                    }

                    // We are going to call Update updateCount times, so we can subtract this from accumulated elapsed game time
                    accumulatedElapsedGameTime = new TimeSpan(accumulatedElapsedGameTime.Ticks - (updateCount * TargetElapsedTime.Ticks));
                    singleFrameElapsedTime = TargetElapsedTime;
                }

                RawTick(singleFrameElapsedTime, updateCount, drawLag / (float)TargetElapsedTime.Ticks, drawFrame);
            }
            catch (Exception ex)
            {
                Log.Error("Unexpected exception", ex);
                throw;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            var nodeFactoryRegistry = Services.GetService<NodeFactoryRegistry>();

            // Ensure all the paths referenced by VL are visible to the effect system
            if (nodeFactoryRegistry != null)
                UpdateShaderPaths(nodeFactoryRegistry);

            base.Update(gameTime);
        }

        protected override void EndDraw(bool present)
        {
            try
            {
                foreach (var r in PendingPresentCalls)
                    r.Present();

                base.EndDraw(present: false);
            }
            finally
            {
                PendingPresentCalls.Clear();
            }
        }

        void UpdateShaderPaths(NodeFactoryRegistry nodeFactoryRegistry)
        {
            if (nodeFactoryRegistry == NodeFactoryRegistry)
                return;

            NodeFactoryRegistry = nodeFactoryRegistry;

            foreach (var path in nodeFactoryRegistry.Paths)
                if (Directory.Exists(Path.Combine(path, "shaders")))
                    EffectSystem.EnsurePathIsVisible(path);
        }
    }
}

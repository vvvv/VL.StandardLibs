using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using VL.Core;
using VL.Stride.Engine;
using VL.Stride.Rendering;

namespace VL.Stride.Games
{
    public class VLGame : Game
    {
        [ThreadStatic]
        private static LogListener logListenerToUse;

        // GetLogListener gets called in base constructor, we therefor need to provide a way to pass it in beforehand
        public static IDisposable SetLogListenerToUseForGame(LogListener logListener)
        {
            logListenerToUse = logListener;
            return Disposable.Create(() => logListenerToUse = null);
        }

        private readonly TimeSpan maximumElapsedTime = TimeSpan.FromMilliseconds(2000.0);
        private TimeSpan accumulatedElapsedGameTime;
        private bool forceElapsedTimeToZero;

        internal readonly SchedulerSystem SchedulerSystem;
        private readonly NodeFactoryRegistry NodeFactoryRegistry;

        internal event EventHandler BeforeDestroy;

        public VLGame(NodeFactoryRegistry nodeFactoryRegistry)
        {
            NodeFactoryRegistry = nodeFactoryRegistry;

            SchedulerSystem = new SchedulerSystem(Services);
            Services.AddService(SchedulerSystem);

#if DEBUG
            GraphicsDeviceManager.DeviceCreationFlags |= DeviceCreationFlags.Debug;
#endif
            // for now we don't let the user decide upon the colorspace
            // as we'd need to either recreate all textures and swapchains in that moment or make sure that these weren't created yet.
            GraphicsDeviceManager.PreferredColorSpace = ColorSpace.Linear;
        }

        protected override LogListener GetLogListener()
        {
            return logListenerToUse ?? base.GetLogListener();
        }

        protected override void Destroy()
        {
            BeforeDestroy?.Invoke(this, EventArgs.Empty);
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
            // Ensure all the paths referenced by VL are visible to the effect system
            UpdateShaderPaths(NodeFactoryRegistry);

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
            if (!knownPaths.SequenceEqual(nodeFactoryRegistry.Paths))
            {
                knownPaths = nodeFactoryRegistry.Paths.ToImmutableArray();

                foreach (var path in nodeFactoryRegistry.Paths)
                    if (Directory.Exists(Path.Combine(path, "shaders")))
                        EffectSystem.EnsurePathIsVisible(path);
            }
        }
        private ImmutableArray<string> knownPaths = ImmutableArray<string>.Empty;
    }
}

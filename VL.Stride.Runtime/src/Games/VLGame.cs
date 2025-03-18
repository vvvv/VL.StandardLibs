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
using VL.Core;
using VL.Lib.Basics.Video;
using VL.Stride.Core;
using VL.Stride.Engine;
using VL.Stride.Rendering;
using DeviceCreationFlags = Stride.Graphics.DeviceCreationFlags;

namespace VL.Stride.Games
{
    public class VLGame : Game, IGraphicsDeviceProvider
    {
        private readonly TimeSpan maximumElapsedTime = TimeSpan.FromMilliseconds(2000.0);
        private TimeSpan accumulatedElapsedGameTime;
        private bool forceElapsedTimeToZero;

        internal readonly SchedulerSystem SchedulerSystem;
        public bool CaptureFrame { get; set; }
        private bool captureInProgress;
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

        GraphicsDeviceType IGraphicsDeviceProvider.Type => GraphicsDevice.Platform == GraphicsPlatform.Direct3D11 ? GraphicsDeviceType.Direct3D11 : GraphicsDeviceType.None;

        nint IGraphicsDeviceProvider.NativePointer => SharpDXInterop.GetNativeDevice(GraphicsDevice) is SharpDX.Direct3D11.Device d3d11 ? d3d11.NativePointer : default;

        bool IGraphicsDeviceProvider.UseLinearColorspace => GraphicsDevice.ColorSpace == ColorSpace.Linear;

        protected override LogListener GetLogListener()
        {
            // Our logging system is already hooked up, we must not do it multiple times!
            return null;
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

            if (CaptureFrame)
            {
                CaptureFrame = false;
                RenderDocConnector.RenderDocManager?.StartFrameCapture(GraphicsDevice, IntPtr.Zero);
                captureInProgress = true;
            }

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

                if (captureInProgress)
                {
                    captureInProgress = false;
                    RenderDocConnector.RenderDocManager?.EndFrameCapture(GraphicsDevice, IntPtr.Zero);
                }
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

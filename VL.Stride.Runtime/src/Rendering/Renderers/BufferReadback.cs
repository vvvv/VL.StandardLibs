using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Graphics;
using Stride.Rendering;
using VL.Stride.Graphics;
using Buffer = Stride.Graphics.Buffer;

namespace VL.Stride.Rendering
{
    /// <summary>
    /// Allow readback a Buffer from GPU to CPU with a frame delay count to avoid blocking read.
    /// </summary>
    /// <typeparam name="T">Pixel struct that should match the input buffer format</typeparam>
    /// <remarks>The input buffer should be small enough to avoid CPU/GPU readback stalling</remarks>
    public class BufferReadback<T> : RendererBase, IDisposable where T : struct
    {
        private readonly List<Buffer> stagingTargets;

        private readonly List<bool> stagingUsed;
        private int currentStagingIndex;
        private T[] result;
        DataPointer pinnedResult;
        SerialDisposable pinDisposer = new SerialDisposable(); 

        public Buffer Input { get; set; }
        Buffer inputBuffer;

        private int frameDelayCount;
        private int previousStageCount;

        private readonly Stopwatch clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferReadback{T}"/> class.
        /// </summary>
        public BufferReadback()
        {
            stagingUsed = new List<bool>();
            stagingTargets = new List<Buffer>();
            FrameDelayCount = 16;
            clock = new Stopwatch();
        }

        public override bool AlwaysRender => true;

        /// <summary>
        /// Gets or sets the number of frame to store before reading back. Default is <c>16</c>.
        /// </summary>
        /// <value>The frame delay count.</value>
        public int FrameDelayCount
        {
            get
            {
                return frameDelayCount;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Expecting a value > 0");
                }

                frameDelayCount = value;
            }
        }

        /// <summary>
        /// Gets a boolean indicating whether a result is available from <see cref="Result"/>.
        /// </summary>
        /// <value>A result available.</value>
        public bool IsResultAvailable { get; private set; }

        /// <summary>
        /// Gets a boolean indicating whether the readback is slow and may be stalling, indicating a <see cref="FrameDelayCount"/> to low or
        /// an input buffer too large for an efficient non-blocking readback.
        /// </summary>
        /// <value>The readback is slow and stalling.</value>
        public bool IsSlow { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether [force get latest blocking].
        /// </summary>
        /// <value><c>true</c> if [force get latest blocking]; otherwise, <c>false</c>.</value>
        public bool ForceGetLatestBlocking { get; set; }

        /// <summary>
        /// Gets the elapsed time to query the result.
        /// </summary>
        /// <value>The elapsed time.</value>
        public TimeSpan ElapsedTime
        {
            get
            {
                return clock.Elapsed;
            }
        }

        /// <summary>
        /// Gets the result pixels, only valid if <see cref="IsResultAvailable"/>
        /// </summary>
        /// <value>The result.</value>
        public T[] Result
        {
            get
            {
                return result;
            }
        }

        public void Reset()
        {
            // Make sure that StagingUsed is reseted
            for (int i = 0; i < stagingUsed.Count; i++)
            {
                stagingUsed[i] = false;
            }
        }

        protected override void DrawInternal(RenderDrawContext context)
        {
            var input = Input;

            // Start the clock
            clock.Restart();

            // Make sure that we have all staging prepared
            EnsureStaging(input);

            // Copy to staging resource
            context.CommandList.Copy(input, stagingTargets[currentStagingIndex]);
            stagingUsed[currentStagingIndex] = true;

            // Read-back to CPU using a ring of staging buffers
            IsResultAvailable = false;
            IsSlow = false;

            if (ForceGetLatestBlocking)
            {
                stagingTargets[currentStagingIndex].GetData(context.CommandList, result);
                IsResultAvailable = true;
                IsSlow = true;
            }
            else
            {
                for (int i = stagingTargets.Count - 1; !IsResultAvailable && i >= 0; i--)
                {
                    var oldStagingIndex = (currentStagingIndex + i) % stagingTargets.Count;
                    var stagingTarget = stagingTargets[oldStagingIndex];

                    // Only try to get data from staging if it has received a copy of the input buffer
                    if (stagingUsed[oldStagingIndex])
                    {
                        // If oldest staging target?
                        if (i == 0)
                        {
                            // Get data blocking (otherwise we would loop without getting any readback if StagingCount is not enough high)
                            stagingTarget.GetData(context.CommandList, pinnedResult);
                            IsSlow = true;
                            IsResultAvailable = true;
                        }
                        else if (stagingTarget.GetData(context.CommandList, pinnedResult, true, 0, 0)) // Get data non-blocking
                        {
                            IsResultAvailable = true;
                        }
                    }
                }

                // Move to next staging target
                currentStagingIndex = (currentStagingIndex + 1) % stagingTargets.Count;
            }

            // Stop the clock.
            clock.Stop();
        }

        private void EnsureStaging(Buffer input)
        {
            // Create all staging buffer if input is changing
            if (inputBuffer != input || previousStageCount != frameDelayCount)
            {
                DisposeStaging();

                if (input != null)
                {
                    // Allocate result data
                    result = new T[input.CalculateElementCount<T>()];
                    var handle = new GCPinner(result);
                    pinDisposer.Disposable = handle;
                    pinnedResult = new DataPointer(handle.Pointer, result.Length * Utilities.SizeOf<T>());

                    for (int i = 0; i < FrameDelayCount; i++)
                    {
                        stagingTargets.Add(input.ToStaging());
                        stagingUsed.Add(false);
                    }
                }

                previousStageCount = frameDelayCount;
                inputBuffer = input;
            }
        }

        private void DisposeStaging()
        {
            foreach (var stagingTarget in stagingTargets)
            {
                stagingTarget.Dispose();
            }
            stagingUsed.Clear();
            stagingTargets.Clear();
        }

        public void Dispose()
        {
            DisposeStaging();
            pinDisposer.Dispose();
        }
    }
}

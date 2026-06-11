#nullable enable
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;
using System.ComponentModel;
using System.Reactive.Disposables;
using VL.Core;
using VL.Lang;

namespace VL.Stride.Rendering.ComputeEffect
{
    /// <summary>
    /// A compute effect dispatcher doing a direct dispatch with the given thread group count.
    /// </summary>
    [ProcessNode(Name = "DirectDispatcher", Category = "Stride.Rendering.Advanced", FragmentSelection = FragmentSelection.Explicit)]
    public sealed class DirectComputeEffectDispatcher : IComputeEffectDispatcher, IDisposable
    {
        // D3D11_CS_DISPATCH_MAX_THREAD_GROUPS_PER_DIMENSION
        private const int maxGroupsPerDim = 65535;

        private readonly NodeContext nodeContext;
        private SerialDisposable? serialDisposable;
        private Int3 threadGroupCount = Int3.One;

        [Fragment]
        public DirectComputeEffectDispatcher(NodeContext nodeContext)
        {
            this.nodeContext = nodeContext;
        }

        /// <summary>
        /// Gets or sets the number of thread groups to dispatch.
        /// </summary>
        [DefaultValue(typeof(Int3), "1, 1, 1")]
        public Int3 ThreadGroupCount
        {
            get { return threadGroupCount; }
            [Fragment]
            set
            {
                if (value != threadGroupCount)
                {
                    if (value.X < 0 || value.Y < 0 || value.Z < 0)
                        throw new ArgumentException("Thread group count must be non-negative.");

                    if (value.X > maxGroupsPerDim || value.Y > maxGroupsPerDim || value.Z > maxGroupsPerDim)
                        SetWarning();
                    else
                        ClearWarning();

                    threadGroupCount = value;
                }

                void SetWarning()
                {
                    var violations = new List<string>(3);
                    if (value.X > maxGroupsPerDim) violations.Add($"X={value.X}");
                    if (value.Y > maxGroupsPerDim) violations.Add($"Y={value.Y}");
                    if (value.Z > maxGroupsPerDim) violations.Add($"Z={value.Z}");
                    (serialDisposable ??= new()).Disposable = nodeContext.AddPersistentMessage(MessageSeverity.Warning,
                        $"ThreadGroupCount exceeds D3D11_CS_DISPATCH_MAX_THREAD_GROUPS_PER_DIMENSION (65535): {string.Join(", ", violations)}. Dispatch behavior is undefined.");
                }

                void ClearWarning()
                {
                    if (serialDisposable != null)
                        serialDisposable.Disposable = null;
                }
            }
        }

        [Fragment]
        public IComputeEffectDispatcher Output => this;

        public void UpdateParameters(ParameterCollection parameters, Int3 threadGroupSize)
        {
            parameters.Set(ComputeShaderBaseKeys.ThreadGroupCountGlobal, ThreadGroupCount);
        }

        public void Dispatch(RenderDrawContext context)
        {
            context.CommandList.Dispatch(ThreadGroupCount.X, ThreadGroupCount.Y, ThreadGroupCount.Z);
        }

        public void Dispose()
        {
            serialDisposable?.Dispose();
        }
    }
}

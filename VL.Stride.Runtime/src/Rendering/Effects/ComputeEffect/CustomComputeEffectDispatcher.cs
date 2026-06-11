#nullable enable
using Stride.Core.Mathematics;
using Stride.Rendering;
using System;
using VL.Core;

namespace VL.Stride.Rendering.ComputeEffect
{
    /// <summary>
    /// A commpute effect dispatcher using a delegate to compute the thread group count.
    /// </summary>
    [ProcessNode(Name = "CustomDispatcher", Category = "Stride.Rendering.Advanced", FragmentSelection = FragmentSelection.Explicit)]
    public sealed class CustomComputeEffectDispatcher : IComputeEffectDispatcher, IDisposable
    {
        readonly DirectComputeEffectDispatcher directComputeEffectDispatcher;

        [Fragment]
        public CustomComputeEffectDispatcher(NodeContext nodeContext)
        {
            directComputeEffectDispatcher = new DirectComputeEffectDispatcher(nodeContext);
        }

        /// <summary>
        /// The selector function to compute the thread group count based on the thread group size defined by the shader.
        /// </summary>
        public Func<Int3, Int3>? ThreadGroupCountsSelector 
        { 
            get; 
            [Fragment] set; 
        }

        [Fragment]
        public IComputeEffectDispatcher Output => this;

        public void UpdateParameters(ParameterCollection parameters, Int3 threadGroupSize)
        {
            directComputeEffectDispatcher.ThreadGroupCount = ThreadGroupCountsSelector?.Invoke(threadGroupSize) ?? Int3.Zero;
            directComputeEffectDispatcher.UpdateParameters(parameters, threadGroupSize);
        }

        public void Dispatch(RenderDrawContext context)
        {
            directComputeEffectDispatcher.Dispatch(context);
        }

        public void Dispose()
        {
            directComputeEffectDispatcher.Dispose();
        }
    }
}

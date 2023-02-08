using Stride.Core.Mathematics;
using Stride.Rendering;
using System;

namespace VL.Stride.Rendering.ComputeEffect
{
    /// <summary>
    /// A commpute effect dispatcher using a delegate to compute the thread group count.
    /// </summary>
    class CustomComputeEffectDispatcher : IComputeEffectDispatcher
    {
        readonly DirectComputeEffectDispatcher directComputeEffectDispatcher = new DirectComputeEffectDispatcher();

        /// <summary>
        /// The selector function to compute the thread group count based on the thread group size defined by the shader.
        /// </summary>
        public Func<Int3, Int3> ThreadGroupCountsSelector { get; set; }

        public void UpdateParameters(ParameterCollection parameters, Int3 threadGroupSize)
        {
            directComputeEffectDispatcher.ThreadGroupCount = ThreadGroupCountsSelector?.Invoke(threadGroupSize) ?? Int3.Zero;
            directComputeEffectDispatcher.UpdateParameters(parameters, threadGroupSize);
        }

        public void Dispatch(RenderDrawContext context)
        {
            directComputeEffectDispatcher.Dispatch(context);
        }
    }
}

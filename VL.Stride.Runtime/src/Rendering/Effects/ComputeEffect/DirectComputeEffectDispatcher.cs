using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;

namespace VL.Stride.Rendering.ComputeEffect
{
    /// <summary>
    /// A compute effect dispatcher doing a direct dispatch with the given thread group count.
    /// </summary>
    class DirectComputeEffectDispatcher : IComputeEffectDispatcher
    {
        /// <summary>
        /// Gets or sets the number of thread groups to dispatch.
        /// </summary>
        public Int3 ThreadGroupCount { get; set; } = Int3.One;

        public void UpdateParameters(ParameterCollection parameters, Int3 threadGroupSize)
        {
            parameters.Set(ComputeShaderBaseKeys.ThreadGroupCountGlobal, ThreadGroupCount);
        }

        public void Dispatch(RenderDrawContext context)
        {
            context.CommandList.Dispatch(ThreadGroupCount.X, ThreadGroupCount.Y, ThreadGroupCount.Z);
        }
    }
}

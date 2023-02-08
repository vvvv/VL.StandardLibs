using Stride.Core.Mathematics;
using Stride.Rendering;

namespace VL.Stride.Rendering.ComputeEffect
{
    /// <summary>
    /// A compute effect dispatcher is used by the compute effects to implement the shader dispatch (direct or indirect).
    /// </summary>
    public interface IComputeEffectDispatcher
    {
        /// <summary>
        /// Updates the parameter collection of the shader.
        /// </summary>
        /// <param name="parameters">The parameter collection of the shader.</param>
        /// <param name="threadGroupSize">The thread group size as defined by the shader in the [numthreads(X, Y, Z)] attribute.</param>
        void UpdateParameters(ParameterCollection parameters, Int3 threadGroupSize);

        /// <summary>
        /// Dispatches the shader.
        /// </summary>
        /// <param name="context">The render draw context.</param>
        void Dispatch(RenderDrawContext context);
    }
}

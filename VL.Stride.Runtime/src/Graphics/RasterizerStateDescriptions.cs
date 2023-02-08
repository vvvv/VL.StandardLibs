using Stride.Graphics;

namespace VL.Stride.Graphics
{
    /// <summary>
    /// Some predefined rasterizer state descriptions.
    /// </summary>
    public static class RasterizerStateDescriptions
    {
        /// <summary>
        /// The default description.
        /// </summary>
        public static readonly RasterizerStateDescription Default = RasterizerStateDescription.Default;

        /// <summary>
        /// Culling of primitives with clockwise winding order.
        /// </summary>
        public static readonly RasterizerStateDescription CullFront = RasterizerStates.CullFront;

        /// <summary>
        /// Culling of primitives with counter-clockwise winding order.
        /// </summary>
        public static readonly RasterizerStateDescription CullBack = RasterizerStates.CullBack;

        /// <summary>
        /// No culling.
        /// </summary>
        public static readonly RasterizerStateDescription CullNone = RasterizerStates.CullNone;

        /// <summary>
        /// Wireframe rendering with culling of primitives in clockwise winding order.
        /// </summary>
        public static readonly RasterizerStateDescription WireframeCullFront = RasterizerStates.WireframeCullFront;

        /// <summary>
        /// Wireframe rendering with culling of primitives in counter-clockwise winding order.
        /// </summary>
        public static readonly RasterizerStateDescription WireframeCullBack = RasterizerStates.WireframeCullBack;

        /// <summary>
        /// Wireframe rendering with no culling.
        /// </summary>
        public static readonly RasterizerStateDescription Wireframe = RasterizerStates.Wireframe;
    }
}

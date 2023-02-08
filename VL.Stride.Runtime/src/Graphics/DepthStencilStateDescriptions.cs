using Stride.Graphics;

namespace VL.Stride.Graphics
{
    /// <summary>
    /// Some predefined depth stencil state descriptions.
    /// </summary>
    public static class DepthStencilStateDescriptions
    {
        /// <summary>
        /// Use a depth stencil buffer.
        /// </summary>
        public static readonly DepthStencilStateDescription Default = DepthStencilStates.Default;

        /// <summary>
        /// Default settings using greater comparison for Z.
        /// </summary>
        public static readonly DepthStencilStateDescription DefaultInverse = DepthStencilStates.DefaultInverse;

        /// <summary>
        /// Enables a read-only depth stencil buffer.
        /// </summary>
        public static readonly DepthStencilStateDescription DepthRead = DepthStencilStates.DepthRead;

        /// <summary>
        /// Don't use a depth stencil buffer.
        /// </summary>
        public static readonly DepthStencilStateDescription None = DepthStencilStates.None;
    }
}

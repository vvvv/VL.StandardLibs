using Stride.Graphics;
using static Stride.Graphics.BlendStateDescription;

namespace VL.Stride.Graphics
{
    /// <summary>
    /// Some predefined blend state descriptions.
    /// </summary>
    public static class BlendStateDescriptions
    {
        static BlendStateDescriptions()
        {
            AlphaBlend = new BlendStateDescription(Blend.SourceAlpha, Blend.InverseSourceAlpha);
            AlphaBlend.RenderTargets[0].AlphaSourceBlend = Blend.One;
            AlphaBlend.RenderTargets[0].AlphaDestinationBlend = Blend.InverseSourceAlpha;

            AlphaBlendPremultiplied = new BlendStateDescription(Blend.One, Blend.InverseSourceAlpha);
        }

        /// <summary>
        /// No blending.
        /// </summary>
        public static readonly BlendStateDescription None = BlendStates.Default;

        /// <summary>
        /// The sourc and destination get added.
        /// </summary>
        /// <remarks>
        /// Color = Src.RGB + Dst.RGB
        /// Alpha = Src.A + Dst.A
        /// </remarks>
        public static readonly BlendStateDescription Additive = new BlendStateDescription(Blend.One, Blend.One);

        /// <summary>
        /// The source and destination get blended using the alpha value of the source assuming straight alpha.
        /// </summary>
        /// <remarks>
        /// Color = Src.RGB * Src.A + Dst.RGB * (1 - Src.A)
        /// Alpha = Src.A + Dst.A * (1 - Src.A)
        /// </remarks>
        public static readonly BlendStateDescription AlphaBlend;

        /// <summary>
        /// The source and destination get blended using the alpha value of the source assuming premultiplied alpha.
        /// </summary>
        /// <remarks>
        /// Color = Src.RGB + Dst.RGB * (1 - Src.A)
        /// Alpha = Src.A + Dst.A * (1 - Src.A)
        /// </remarks>
        public static readonly BlendStateDescription AlphaBlendPremultiplied;
    }
}

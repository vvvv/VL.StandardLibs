using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace VL.Stride.Rendering
{
    /// <summary>
    /// Enables mesh outline (MeshOutlineRenderFeature must be enabled in Graphics Compositor)
    /// </summary>
    [DataContract("MeshOutline")]
    [Display("Outline", null, Expand = ExpandRule.Once)]
    [ComponentCategory("Model")]
    public class MeshOutlineComponent : ActivableEntityComponent
    {
        /// <summary>
        /// Color of outline
        /// </summary>
        [DataMember(10)]
        public Color4 Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f);

        /// <summary>
        /// Intensity of outline color
        /// </summary>
        [DataMember(20)]
        public float Intensity = 1.0f;

        /// <summary>
        /// Site of outline
        /// </summary>
        [DataMember(30)]
        [DataMemberRange(0.0f, 1.0f, 0.01f, 0.5f, 2)]
        public float Size = 0.2f;
    }
}

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace VL.Stride.Rendering
{
    /// <summary>
    /// Enables wireframe over mesh (SinglePassWireframeRenderFeature must be enabled in Graphics Compositor)
    /// </summary>
    [DataContract("Wireframe")]
    [Display("Wireframe", null, Expand = ExpandRule.Once)]
    [ComponentCategory("Model")]
    public class WireframeComponent : ActivableEntityComponent
    {
        /// <summary>
        /// Width of wireframe lines
        /// </summary>
        [DataMember(10)]
        [DataMemberRange(0.0, 10.0, 0.01f, 1.0f, 2)]
        public float LineWidth = 1.0f;

        /// <summary>
        /// Color of wireframe lines
        /// </summary>
        [DataMember(20)]
        public Color4 Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
    }
}

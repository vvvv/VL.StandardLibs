using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Rendering;
using System.ComponentModel;

namespace VL.Stride.Rendering
{
    public enum DrawerRenderStage
    {
        BeforeScene,
        Opaque,
        Transparent,
        AfterScene,
        ShadowCaster,
        ShadowCasterParaboloid,
        ShadowCasterCubeMap
    }

    /// <summary>
    /// Renderer components get picked up by the <see cref="EntityRendererProcessor"/> for low level rendering.
    /// </summary>
    [DataContract(nameof(EntityRendererComponent))]
    [DefaultEntityComponentRenderer(typeof(EntityRendererProcessor))]
    public sealed class EntityRendererComponent : ActivableEntityComponent
    {
        /// <summary>
        /// Gets or sets a value indicating whether this renderer should only render once per frame.
        /// i.e. not for each eye in a VR rendering setup.
        /// </summary>
        [DataMember(10)]
        public bool SingleCallPerFrame { get; set; }

        /// <summary>
        /// Gets or sets a value indicating on which render stage this renderer should be rendered.
        /// </summary>
        [DataMember(20)]
        public DrawerRenderStage RenderStage { get; set; } = DrawerRenderStage.Opaque;

        /// <summary>
        /// The render group for this component.
        /// </summary>
        [DataMember(30)]
        [DefaultValue(RenderGroup.Group0)]
        public RenderGroup RenderGroup { get; set; }

        public EntityRendererComponent()
        {

        }

        /// <summary>
        /// The renderer which does the rendering.
        /// </summary>
        [DataMemberIgnore]
        public IGraphicsRendererBase Renderer { get; set; }
    }
}

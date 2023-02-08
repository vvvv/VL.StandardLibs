using Stride.Rendering;
using System.ComponentModel;

namespace VL.Stride.Rendering
{
    public class EntityRendererStageSelector : RenderStageSelector
    {
        [DefaultValue(RenderGroupMask.All)]
        public RenderGroupMask RenderGroup { get; set; } = RenderGroupMask.All;

        public RenderStage BeforeScene { get; set; }
        public RenderStage InSceneOpaque { get; set; }
        public RenderStage InSceneTransparent { get; set; }
        public RenderStage AfterScene { get; set; }
        public RenderStage ShadowCaster { get; set; }
        public RenderStage ShadowCasterParaboloid { get; set; }
        public RenderStage ShadowCasterCubeMap { get; set; }

        public override void Process(RenderObject renderObject)
        {
            if (((RenderGroupMask)(1U << (int)renderObject.RenderGroup) & RenderGroup) != 0)
            {
                var renderDrawer = (RenderRenderer)renderObject;

                var renderStage = SelectRenderStage(renderDrawer);

                if (renderStage != null)
                    renderObject.ActiveRenderStages[renderStage.Index] = new ActiveRenderStage("Test");
            }
        }

        private RenderStage SelectRenderStage(RenderRenderer renderDrawer)
        {
            switch (renderDrawer.RenderStage)
            {
                case DrawerRenderStage.BeforeScene:
                    return BeforeScene;
                case DrawerRenderStage.Opaque:
                    return InSceneOpaque;
                case DrawerRenderStage.Transparent:
                    return InSceneTransparent;
                case DrawerRenderStage.AfterScene:
                    return AfterScene;
                case DrawerRenderStage.ShadowCaster:
                    return ShadowCaster;
                case DrawerRenderStage.ShadowCasterParaboloid:
                    return ShadowCasterParaboloid;
                case DrawerRenderStage.ShadowCasterCubeMap:
                    return ShadowCasterCubeMap;
                default:
                    return InSceneOpaque;
            }
        }
    }
}

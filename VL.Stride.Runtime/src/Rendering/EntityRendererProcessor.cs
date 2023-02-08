using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Rendering;

namespace VL.Stride.Rendering
{
    /// <summary>
    /// The entity renderer processor installs for each <see cref="EntityRendererComponent"/> a <see cref="RenderRenderer"/> object in its visibility group.
    /// </summary>
    public class EntityRendererProcessor : EntityProcessor<EntityRendererComponent, EntityRendererProcessor.RendererData>, IEntityComponentRenderProcessor
    {
        public class RendererData
        {
            public EntityRendererComponent RenderComponent;
            public RenderRenderer RenderRenderer;
        }

        public VisibilityGroup VisibilityGroup { get; set; }

        protected override RendererData GenerateComponentData([NotNull] Entity entity, [NotNull] EntityRendererComponent component)
        {
            var data = new RendererData() { RenderComponent = component };
            return data;
        }

        public override void Draw(RenderContext context)
        {
            // Go thru components
            foreach (var entityKeyPair in ComponentDatas)
            {
                var component = entityKeyPair.Key;
                var rendererData = entityKeyPair.Value;

                // Component was just added
                if (rendererData.RenderRenderer == null)
                {
                    CreateAndAddRenderObject(rendererData, component);
                }

                // Stage or group has changed
                if (rendererData.RenderRenderer.RenderStage != component.RenderStage 
                    || rendererData.RenderRenderer.RenderGroup != component.RenderGroup)
                {
                    VisibilityGroup.RenderObjects.Remove(rendererData.RenderRenderer);
                    CreateAndAddRenderObject(rendererData, component);
                }

                var renderRenderer = rendererData.RenderRenderer;
                renderRenderer.Enabled = component.Enabled && component.Renderer != null;
                if (renderRenderer.Enabled)
                {
                    renderRenderer.SingleCallPerFrame = component.SingleCallPerFrame;
                    renderRenderer.ParentTransformation = component.Entity.Transform.WorldMatrix;
                    renderRenderer.Renderer = component.Renderer;
                }
            }

            base.Draw(context);
        }

        private void CreateAndAddRenderObject(RendererData rendererData, EntityRendererComponent component)
        {
            rendererData.RenderRenderer = new RenderRenderer() { RenderStage = component.RenderStage, RenderGroup = component.RenderGroup };
            VisibilityGroup.RenderObjects.Add(rendererData.RenderRenderer);
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] EntityRendererComponent component, [NotNull] RendererData data)
        {
            base.OnEntityComponentAdding(entity, component, data);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] EntityRendererComponent component, [NotNull] RendererData data)
        {
            if (data.RenderRenderer != null)
                VisibilityGroup.RenderObjects.Remove(data.RenderRenderer);

            base.OnEntityComponentRemoved(entity, component, data);
        }
    }
}

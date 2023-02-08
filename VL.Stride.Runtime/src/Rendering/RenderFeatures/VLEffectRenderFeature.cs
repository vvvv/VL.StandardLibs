using Stride.Rendering;

namespace VL.Stride.Rendering
{
    /// <summary>
    /// Custom render feature, that manages the VLEffectMain effect
    /// </summary>
    public class VLEffectRenderFeature : SubRenderFeature
    {
        private StaticObjectPropertyKey<RenderEffect> renderEffectKey;

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            base.InitializeCore();

            renderEffectKey = ((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey;
        }

        /// <inheritdoc/>
        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            var renderEffects = RootRenderFeature.RenderData.GetData(renderEffectKey);
            int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;

            foreach (var renderObject in RootRenderFeature.RenderObjects)
            {
                var staticObjectNode = renderObject.StaticObjectNode;
                var renderMesh = (RenderMesh)renderObject;

                for (int i = 0; i < effectSlotCount; ++i)
                {
                    var staticEffectObjectNode = staticObjectNode * effectSlotCount + i;
                    var renderEffect = renderEffects[staticEffectObjectNode];

                    // Skip effects not used during this frame
                    if (renderEffect == null || !renderEffect.IsUsedDuringThisFrame(RenderSystem))
                        continue;

                    // Generate shader permuatations
                    var enableBySource = renderMesh.MaterialPass.Parameters.Get(VLEffectParameters.EnableExtensionShader);
                    if (enableBySource)
                    {
                        renderEffect.EffectValidator.ValidateParameter(VLEffectParameters.EnableExtensionShader, enableBySource);
                        renderEffect.EffectValidator.ValidateParameter(VLEffectParameters.MaterialExtensionShader, renderMesh.MaterialPass.Parameters.Get(VLEffectParameters.MaterialExtensionShader));
                    }

                    var enableByNameMesh = renderMesh.Mesh.Parameters.Get(VLEffectParameters.EnableExtensionNameMesh);
                    if (enableByNameMesh)
                    {
                        renderEffect.EffectValidator.ValidateParameter(VLEffectParameters.EnableExtensionNameMesh, enableByNameMesh);
                        renderEffect.EffectValidator.ValidateParameter(VLEffectParameters.MaterialExtensionNameMesh, renderMesh.Mesh.Parameters.Get(VLEffectParameters.MaterialExtensionNameMesh));
                    }

                    var enableBySourceMesh = renderMesh.Mesh.Parameters.Get(VLEffectParameters.EnableExtensionShaderMesh);
                    if (enableBySourceMesh)
                    {
                        renderEffect.EffectValidator.ValidateParameter(VLEffectParameters.EnableExtensionShaderMesh, enableBySourceMesh);
                        renderEffect.EffectValidator.ValidateParameter(VLEffectParameters.MaterialExtensionShaderMesh, renderMesh.Mesh.Parameters.Get(VLEffectParameters.MaterialExtensionShaderMesh));
                    }
                }
            }
        }
    }
}

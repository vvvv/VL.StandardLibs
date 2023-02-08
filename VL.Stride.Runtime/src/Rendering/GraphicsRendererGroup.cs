using System;
using System.Collections.Generic;
using VL.Lib.Collections;
using VL.Stride.Rendering;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Core;

namespace VL.Stride.Rendering
{
    /// <summary>
    /// Groups a sequence of <see cref="IGraphicsRendererBase"/> into one.
    /// </summary>
    public sealed class GraphicsRendererGroup : IGraphicsRendererBase
    {
        Spread<IGraphicsRendererBase> upstreamRenderers = Spread<IGraphicsRendererBase>.Empty;

        public void Update(Spread<IGraphicsRendererBase> input)
        {
            upstreamRenderers = input;
        }

        void IGraphicsRendererBase.Draw(RenderDrawContext context)
        {
            foreach (var upstreamLayer in upstreamRenderers)
                upstreamLayer?.Draw(context);
        }
    }

    public interface IRenderContextModifier
    {
        IDisposable ModifyRenderContext(RenderContext renderContext);
    }

    public class ParentTransformationModifier : IRenderContextModifier
    {
        private Matrix currentParentTransformation = Matrix.Identity;
        private Matrix transformation = Matrix.Identity;

        public Matrix Transformation { get => transformation; set => transformation = value; }

        public ModelTransformUsage ExistingTransformUsage { get; set; } = ModelTransformUsage.Ignore;

        public IDisposable ModifyRenderContext(RenderContext renderContext)
        {
            switch (ExistingTransformUsage)
            {
                case ModelTransformUsage.Ignore:
                    return renderContext.PushTagAndRestore(EntityRendererRenderFeature.CurrentParentTransformation, transformation);
                case ModelTransformUsage.PreMultiply:
                    var p = renderContext.Tags.Get(EntityRendererRenderFeature.CurrentParentTransformation);
                    Matrix.Multiply(ref p, ref transformation, out currentParentTransformation);
                    return renderContext.PushTagAndRestore(EntityRendererRenderFeature.CurrentParentTransformation, currentParentTransformation);
                case ModelTransformUsage.PostMultiply:
                    p = renderContext.Tags.Get(EntityRendererRenderFeature.CurrentParentTransformation);
                    Matrix.Multiply(ref transformation, ref p, out currentParentTransformation);
                    return renderContext.PushTagAndRestore(EntityRendererRenderFeature.CurrentParentTransformation, currentParentTransformation);
                default:
                    throw new NotImplementedException();
            }
        }
    }


    public class RenderContextModifierRenderer : RendererBase
    {
        public IRenderContextModifier Modifier { get; set; }

        protected override void DrawInternal(RenderDrawContext context)
        {
            if (Modifier is null)
            {
                DrawInput(context);
            }
            else
            {
                using (Modifier.ModifyRenderContext(context.RenderContext))
                {
                    Input?.Draw(context);
                }
            }
        }
    }
}

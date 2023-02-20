using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using VL.Core;

namespace VL.Stride.Rendering
{

    /// <summary>
    /// The render feature redirects low level rendering calls to the <see cref="IGraphicsRendererBase"/> 
    /// </summary>
    public class EntityRendererRenderFeature : RootRenderFeature
    {
        /// <summary>
        /// A property key to get the current parent transformation from the <see cref="ComponentBase.Tags"/> property of the render context.
        /// </summary>
        public static readonly PropertyKey<Matrix> CurrentParentTransformation = new PropertyKey<Matrix>("EntityRendererRenderFeature.CurrentParentTransformation", typeof(Matrix), DefaultValueMetadata.Static(Matrix.Identity, keepDefaultValue: true));

        private readonly List<RenderRenderer> singleCallRenderers = new List<RenderRenderer>();
        private readonly List<RenderRenderer> renderers = new List<RenderRenderer>();
        private int lastFrameNr;
        private IVLRuntime runtime;

        public RenderStage HelpersRenderStage { get; set; } // TODO: shouldn't be a pin
        public IGraphicsRendererBase HelpersRenderer { get; set; }

        public EntityRendererRenderFeature()
        {
            // Pre adjust render priority, low numer is early, high number is late (advantage of backbuffer culling)
            SortKey = 190;
        }

        public override void Prepare(RenderDrawContext context)
        {
            base.Prepare(context);
            runtime ??= context.RenderContext.Services.GetService<IVLRuntime>();
        }

        public override Type SupportedRenderObjectType => typeof(RenderRenderer);


        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage)
        {
            base.Draw(context, renderView, renderViewStage);

            if (HelpersRenderStage != null && HelpersRenderer != null && renderViewStage.Index == HelpersRenderStage.Index)
            {
                HelpersRenderer.Draw(context);
            }
        }

        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
        {
            //CPU and GPU profiling
            using (Profiler.Begin(VLProfilerKeys.InSceneRenderProfilingKey))
            using (context.QueryManager.BeginProfile(Color.Green, VLProfilerKeys.InSceneRenderProfilingKey))
            {
                // Do not call into VL if not running
                if (runtime != null && !runtime.IsRunning)
                    return;

                // Build the list of renderers to render
                singleCallRenderers.Clear();
                renderers.Clear();
                for (var index = startIndex; index < endIndex; index++)
                {
                    var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
                    var renderNode = GetRenderNode(renderNodeReference);
                    var renderRenderer = (RenderRenderer)renderNode.RenderObject;

                    if (renderRenderer.Enabled)
                    {
                        if (renderRenderer.SingleCallPerFrame)
                            singleCallRenderers.Add(renderRenderer);
                        else
                            renderers.Add(renderRenderer);
                    }
                }

                if (singleCallRenderers.Count == 0 && renderers.Count == 0)
                    return;

                using (context.RenderContext.PushRenderViewAndRestore(renderView))
                {
                    // Call renderers which want to get invoked only once per frame first
                    var currentFrameNr = context.RenderContext.Time.FrameCount;
                    if (lastFrameNr != currentFrameNr)
                    {
                        lastFrameNr = currentFrameNr;
                        foreach (var renderer in singleCallRenderers)
                        {
                            try
                            {
                                using (context.RenderContext.PushTagAndRestore(CurrentParentTransformation, renderer.ParentTransformation))
                                {
                                    renderer.Renderer?.Draw(context);
                                }
                            }
                            catch (Exception e)
                            {
                                RuntimeGraph.ReportException(e);
                            }
                        }
                    }

                    // Call renderers which can get invoked twice per frame (for each eye)
                    foreach (var renderer in renderers)
                    {
                        try
                        {
                            using (context.RenderContext.PushTagAndRestore(CurrentParentTransformation, renderer.ParentTransformation))
                            {
                                renderer.Renderer?.Draw(context);
                            }
                        }
                        catch (Exception e)
                        {
                            RuntimeGraph.ReportException(e);
                        }
                    } 
                }
            }
        }
    }
}

using Stride.Graphics;
using Stride.Rendering.Compositing;
using System;
using System.Threading.Tasks;
using VL.Stride.Engine;

namespace VL.Stride.Graphics
{
    internal static class GraphicsResourceUtils
    {
        public static async Task<T> CopyToStagingAsync<T>(this GraphicsResource resource, T stagingResource, SchedulerSystem schedulerSystem)
            where T : GraphicsResource
        {
            if (resource is null)
                throw new ArgumentNullException(nameof(resource));
            if (schedulerSystem is null)
                throw new ArgumentNullException(nameof(schedulerSystem));

            var tcs = new TaskCompletionSource<T>();

            DelegateSceneRenderer resultAwaiter = null;

            var copyRenderer = new DelegateSceneRenderer(ctx =>
            {
                try
                {
                    var commandList = ctx.CommandList;
                    commandList.Copy(resource, stagingResource);
                    schedulerSystem.Schedule(resultAwaiter);
                }
                catch (Exception e)
                {
                    stagingResource.Dispose();
                    tcs.SetException(e);
                }
            });

            resultAwaiter = new DelegateSceneRenderer(ctx =>
            {
                try
                {
                    var commandList = ctx.CommandList;
                    var mappedResource = commandList.MapSubresource(stagingResource, 0, MapMode.Read, doNotWait: true);
                    if (mappedResource.DataBox.IsEmpty)
                    {
                        // Try again in next frame
                        schedulerSystem.Schedule(resultAwaiter);
                    }
                    else
                    {
                        commandList.UnmapSubresource(mappedResource);
                        tcs.SetResult(stagingResource);
                    }
                }
                catch (Exception e)
                {
                    stagingResource.Dispose();
                    tcs.SetException(e);
                }
            });

            schedulerSystem.Schedule(copyRenderer);

            return await tcs.Task;
        }
    }
}

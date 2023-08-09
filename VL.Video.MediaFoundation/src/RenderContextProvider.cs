using Stride.Graphics;
using System;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Skia;
using ServiceRegistry = VL.Core.ServiceRegistry;

namespace VL.Video.MediaFoundation
{
    internal class RenderContextProvider
    {
        public static RenderContext GetSkiaRenderContext()
        {
            return RenderContext.ForCurrentThread();
        }

        public static (RenderContext, IDisposable) GetStrideRenderContext(int msaaSamples = 1)
        {
            var m = typeof(VL.Stride.Windows.SkiaRenderer).GetMethod("GetInteropContext", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var deviceProvider = AppHost.Current.Services.GetService<IResourceProvider<GraphicsDevice>>();
            var graphicsDeviceHandle = deviceProvider.GetHandle();
            var ctx = m.Invoke(null, new object[] { graphicsDeviceHandle.Resource, msaaSamples });
            var f = ctx.GetType().GetField("SkiaRenderContext", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var r = f.GetValue(ctx) as RenderContext;
            r.AddRef();
            return (r, graphicsDeviceHandle);
        }
    }
}

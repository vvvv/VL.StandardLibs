using SharpDX.Direct3D11;
using System;
using VL.Core;
using VL.Skia;

namespace VL.Video.MediaFoundation
{
    public sealed class SkiaDeviceProvider : DeviceProvider
    {
        private readonly RenderContext renderContext;
        private readonly IDisposable graphicsHandle;

        public SkiaDeviceProvider(bool useStrideDevice)
        {
            if (useStrideDevice)
                (renderContext, graphicsHandle) = RenderContextProvider.GetStrideRenderContext();
            else
                renderContext = RenderContextProvider.GetSkiaRenderContext();

            if (renderContext.EglContext.Dislpay.TryGetD3D11Device(out var d3dDevice))
            {
                Device = new Device(d3dDevice);
            }
        }

        public override Device Device { get; }

        public override bool UsesLinearColorspace => false;

        public override void Dispose()
        {
            renderContext.Dispose();
            graphicsHandle?.Dispose();
        }
    }
}

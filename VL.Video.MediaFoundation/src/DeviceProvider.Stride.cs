using SharpDX.Direct3D11;
using Stride.Core;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using System;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Stride;

namespace VL.Video.MediaFoundation
{
    public sealed class StrideDeviceProvider : DeviceProvider
    {
        private readonly IResourceHandle<RenderDrawContext> renderDrawContextHandle;
        private readonly GraphicsDevice graphicsDevice;

        public StrideDeviceProvider()
        {
            renderDrawContextHandle = AppHost.Current.Services.GetGameProvider()
                .Bind(g => RenderContext.GetShared(g.Services).GetThreadContext())
                .GetHandle() ?? throw new ServiceNotFoundException(typeof(IResourceProvider<Game>));

            graphicsDevice = renderDrawContextHandle.Resource.GraphicsDevice;
            Device = SharpDXInterop.GetNativeDevice(graphicsDevice) as Device;
        }

        public override Device Device { get; }

        public override bool UsesLinearColorspace => graphicsDevice.ColorSpace == ColorSpace.Linear;

        public override void Dispose()
        {
            renderDrawContextHandle.Dispose();
        }
    }
}

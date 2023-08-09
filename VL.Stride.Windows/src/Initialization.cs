using Stride.Graphics;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Lib.Basics.Resources;

[assembly: AssemblyInitializer(typeof(VL.Stride.Windows.Core.Initialization))]

namespace VL.Stride.Windows.Core
{
    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        public override void Configure(AppHost appHost)
        {
            // VL.MediaFoundation asks for a Direct3D11 device
            appHost.Services.RegisterProvider(game => ResourceProvider.Return(SharpDXInterop.GetNativeDevice(game.GraphicsDevice) as SharpDX.Direct3D11.Device));
        }
    }
}
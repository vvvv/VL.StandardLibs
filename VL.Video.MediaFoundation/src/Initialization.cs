using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Stride.Core;
using Stride.Graphics;
using System.Collections.Immutable;
using System.Linq;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Lib.Basics.Resources;
using DeviceCreationFlags = SharpDX.Direct3D11.DeviceCreationFlags;

[assembly: AssemblyInitializer(typeof(VL.Video.MediaFoundation.Initialization))]

namespace VL.Video.MediaFoundation
{
    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        public override void Configure(AppHost appHost)
        {
            var factory = appHost.Factory;

            if (!factory.HasService<NodeContext, IResourceProvider<Device>>())
            {
                factory.RegisterService<NodeContext, IResourceProvider<Device>>(nodeContext =>
                {
                    // One per entry point
                    return ResourceProvider.NewPooledPerApp(() => new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport));
                });
            }

            factory.RegisterService<IRefCounter<Texture>>(TextureRefCounter.Default);
            factory.RegisterService<IRefCounter<VideoFrame>>(VideoFrameRefCounter.Default);

            appHost.RegisterNodeFactory("VL.Video.MediaFoundation.ControlNodes", f =>
            {
                // TODO: fragmented = true causes troubles on disconnect, apparently the default value injection can't deal with nullables -> injects 0 instead of null
                var nodes = ImmutableArray.Create(
                    f.NewNodeDescription(nameof(CameraControls), "Video.MediaFoundation", fragmented: false, bc =>
                    {
                        return bc.Node(
                            inputs: CameraControls.Default.Properties.Select(p => Pin(bc, p.Name.ToString())),
                            outputs: new[] { bc.Pin("Output", typeof(CameraControls)) },
                            newNode: ibc =>
                            {
                                var controls = new CameraControls();
                                return ibc.Node(
                                    inputs: controls.Properties,
                                    outputs: new[] { ibc.Output(() => controls) });
                            },
                            summary: "Controls camera parameters like zoom, exposure,...",
                            remarks: "Connects to the VideoIn node.\r\nNote that not all cameras will support all of the available properties"
                            );
                    }),
                    f.NewNodeDescription(nameof(VideoControls), "Video.MediaFoundation", fragmented: false, bc =>
                    {
                        return bc.Node(
                            inputs: VideoControls.Default.Properties.Select(p => Pin(bc, p.Name.ToString())),
                            outputs: new[] { bc.Pin("Output", typeof(VideoControls)) },
                            newNode: ibc =>
                            {
                                var controls = new VideoControls();
                                return ibc.Node(
                                    inputs: controls.Properties,
                                    outputs: new[] { ibc.Output(() => controls) });
                            },
                            summary: "Controls video parameters like brightness, contrast,...",
                            remarks: "Connects to the VideoIn node.\r\nNote that not all cameras will support all of the available properties"
                            );
                    }));
                return NodeBuilding.NewFactoryImpl(nodes);
            });
        }

        static IVLPinDescription Pin(NodeBuilding.NodeDescriptionBuildContext bc, string name) => bc.Pin(name, typeof(Optional<float>));
    }
}

using VL.Lib.Reactive;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;

namespace VL.Core.Reactive
{
    public record ChannelBuildDescription(string name, Type type);


    public static class ChannelHubNodeBuilding
    {
        public static Channel<object> ReScanGlobaChannels = new Channel<object>();

        internal static void RegisterGeneratedNodes(IVLFactory factory)
        {
            var descriptions = new List<ChannelBuildDescription>
            {
                new ChannelBuildDescription("A", typeof(float)),
                new ChannelBuildDescription("B", typeof(object))
            };

            factory.RegisterNodeFactory(NodeBuilding.NewNodeFactory(factory, "VL.CoreLib.GlobalsChannels", descfactory =>
            {
                var nodes = descriptions.Select(d => GetNodeDescription(descfactory, d)).ToImmutableArray();
                return NodeBuilding.NewFactoryImpl(nodes, forPath: p =>
                {
                    var path = Path.Combine(p, "GlobalChannels.txt");
                    if (!File.Exists(path))
                        return null;

                    return _ =>
                    {
                        var lines = File.ReadLines(path);
                        return NodeBuilding.NewFactoryImpl(GetProperDescriptions(factory, descfactory, lines).ToImmutableArray());
                    };
                });
            }));

            IEnumerable<IVLNodeDescription> GetProperDescriptions(IVLFactory factory, IVLNodeDescriptionFactory descfactory, IEnumerable<string> lines)
            {
                foreach (var l in lines)
                {
                    var _ = l.Split(':');
                    var t = factory.GetTypeByName(_[1]);
                    if (t != null)
                        yield return GetNodeDescription(descfactory, new ChannelBuildDescription(name: _[0], t));
                }
            }
        }

        internal static IVLNodeDescription GetNodeDescription(IVLNodeDescriptionFactory descfactory, ChannelBuildDescription channelBuildDescription)
        {
            return descfactory.NewNodeDescription(
                channelBuildDescription.name, 
                "Reactive.GlobalChannels", 
                fragmented: false, 
                invalidated: ReScanGlobaChannels, 
                init: context =>
            {
                var c = Channel.CreateChannelOfType(channelBuildDescription.type);
                var _inputs = new IVLPinDescription[]
                {
                };
                var _outputs = new[]
                {
                    context.Pin("Channel", c.GetType()),
                    context.Pin("Value", channelBuildDescription.type),
                };
                return context.Node(_inputs, _outputs, buildcontext =>
                {
                    var inputs = new IVLPin[]
                    {
                    };
                    var outputs = new IVLPin[]
                    {
                        buildcontext.Output(() => c),
                        buildcontext.Output(() => c.Object),
                    };
                    return buildcontext.Node(inputs, outputs);
                }, summary: @"");
            }, tags: "");
        }
    }
}

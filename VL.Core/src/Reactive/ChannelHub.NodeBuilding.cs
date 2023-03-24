using VL.Lib.Reactive;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;
using System.Reactive.Linq;

namespace VL.Core.Reactive
{
    public static class ChannelHubNodeBuilding
    {
        internal static void RegisterChannelHubNodeFactoryTriggeredViaConfigFile(IVLFactory factory)
        {
            // the ChannelHub in question is not available at compile time.
            // if there would be always be only one, we could react on channelhub changes immediatly.
            // as we opted for one channelhub per entry-point & configfiles, we only can react on file changes for now.
            
            // we can't react on renames like this. would be nice if that would be an option in the future (and replace node applications).
            // for that we'd need either:
            // * a guid per channel
            // * or a channel being described by an element in the document (with serializedID) instead of via config file
            // * or access to a global channel hub (not per entry-point) + subscribing to an actual rename event at that live channelhub.

            var descriptions = new List<ChannelBuildDescription>();
            factory.RegisterNodeFactory(NodeBuilding.NewNodeFactory(factory, "VL.CoreLib.GlobalsChannels", descfactory =>
            {
                var nodes = descriptions.Select(d => GetNodeDescription(descfactory, d, invalidateChannelNode: default, null)).ToImmutableArray();
                return NodeBuilding.NewFactoryImpl(nodes, forPath: p =>
                {
                    var path = ChannelHubConfigWatcher.GetConfigFilePath(p); 
                    if (!File.Exists(path))
                        return null;

                    return _ =>
                    {
                        var watcher = ChannelHubConfigWatcher.GetWatcherForPath(path);
                        return NodeBuilding.NewFactoryImpl(
                            nodes:       watcher.Descriptions.Value.Select(cd => GetNodeDescription(descfactory, cd, invalidateChannelNode: default, watcher)).ToImmutableArray(), 
                            invalidated: watcher.Descriptions.Skip(1));
                    };
                });
            }));
        }

        internal static IVLNodeDescription GetNodeDescription(IVLNodeDescriptionFactory descfactory, 
            ChannelBuildDescription channelBuildDescription, IObservable<object> invalidateChannelNode, ChannelHubConfigWatcher watcher)
        {
            return descfactory.NewNodeDescription(
                channelBuildDescription.Name, 
                "Reactive.GlobalChannels", 
                fragmented: false, 
                invalidated: invalidateChannelNode, 
                init: context =>
            {
                var _inputs = new IVLPinDescription[]
                {
                };
                var _outputs = new[]
                {
                    context.Pin("Channel", Channel.CreateChannelOfType(channelBuildDescription.Type).GetType()),
                    context.Pin("Value", channelBuildDescription.Type),
                }; 

                return context.Node(_inputs, _outputs, buildcontext =>
                {
                    var c = IChannelHub.HubForApp.TryAddChannel(channelBuildDescription.Name, channelBuildDescription.Type);
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

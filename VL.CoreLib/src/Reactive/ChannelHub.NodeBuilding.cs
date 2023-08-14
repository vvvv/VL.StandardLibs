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
        internal static void RegisterChannelHubNodeFactoryTriggeredViaConfigFile(AppHost appHost)
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
            appHost.NodeFactoryRegistry.RegisterNodeFactory(appHost.NodeFactoryCache.GetOrAdd("VL.CoreLib.GlobalsChannels", descfactory =>
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
            var parts = channelBuildDescription.Name.Split('.');
            var nodeName = parts.Last();
            var category = string.Join('.', parts.SkipLast(1));

            return descfactory.NewNodeDescription(
                nodeName, 
                string.IsNullOrEmpty(category) ? "Reactive.GlobalChannels" : $"Reactive.GlobalChannels.{category}",
                fragmented: true, 
                invalidated: invalidateChannelNode, 
                init: context =>
            {
                var type = channelBuildDescription.CompileTimeType;
                var _inputs = new IVLPinDescription[]
                {                   
                    context.Pin("Value", type),
                };
                var _outputs = new[]
                {
                    context.Pin("Channel", typeof(IChannel<>).MakeGenericType(type)),
                    context.Pin("Value", type),
                }; 

                return context.Node(_inputs, _outputs, buildcontext =>
                {
                    var channelType = channelBuildDescription.RuntimeType;
                    var c = IChannelHub.HubForApp.TryAddChannel(channelBuildDescription.Name, channelType);

                    bool refetched()
                    {
                        var oc = c;
                        c = IChannelHub.HubForApp.TryGetChannel(channelBuildDescription.Name) ?? 
                            IChannelHub.HubForApp.TryAddChannel(channelBuildDescription.Name, channelType);
                        return c != oc;
                    }

                    Optional<object> latestValueThatGotSet = default;
                    var inputs = new IVLPin[]
                    {
                        buildcontext.Input<object>(value =>
                        {
                            if (refetched() || !latestValueThatGotSet.HasValue || !value.Equals(latestValueThatGotSet.Value))
                            {
                                c.Object = value;
                                latestValueThatGotSet = value;
                            }
                        })
                    };
                    var outputs = new IVLPin[]
                    {
                        buildcontext.Output(() => { refetched(); return c; }),
                        buildcontext.Output(() => { refetched(); return c.Object; }),
                    };
                    return buildcontext.Node(inputs, outputs);
                }, summary: @"");
            }, tags: "");
        }
    }
}

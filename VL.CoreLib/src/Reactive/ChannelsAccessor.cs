#nullable enable
using System;
using System.Collections.Generic;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Reactive;

[assembly: ImportType(typeof(ChannelsAccessor<>), Name = "ChannelsAccessor", Category = "Reactive.Channel.Advanced")]

namespace VL.Lib.Reactive;

/// <summary>
/// Registers the surrounding node as accessor of the given channels. Used by nodes which publish or bind channels
/// without a PublicChannel node, like Publish. Registered channels are considered in use (the public channel browser doesn't
/// remove them) and the public channel browser can jump to the surrounding node.
/// </summary>
/// <typeparam name="TChannel">The channel type</typeparam>
[ProcessNode]
public sealed class ChannelsAccessor<TChannel> : IDisposable
    where TChannel : class, IChannel
{
    // Reference equality, channels don't override Equals.
    readonly HashSet<IChannel> registered = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<IChannel> current = new(ReferenceEqualityComparer.Instance);
    readonly NodePath accessor;

    /// <param name="nodeContext">The context of this node. The nodes the channels get registered for is the one that encloses this node.</param>
    public ChannelsAccessor([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    {
        // The node which contains this one is the one the user knows about, e.g. the Publish node.
        accessor = new NodePath(nodeContext.Path.Stack.Pop());
    }

    /// <summary>
    /// Registers the surrounding node as accessor of all given channels and unregisters it from those which are gone.
    /// </summary>
    /// <param name="channels">The channels the surrounding node accesses.</param>
    public void Update(IEnumerable<TChannel>? channels)
    {
        current.Clear();
        if (channels != null)
        {
            foreach (var channel in channels)
            {
                if (channel is null)
                    continue;
                current.Add(channel);
                if (registered.Add(channel))
                    channel.AccessorNodes.AddAccessorNode(accessor);
            }
        }

        if (registered.Count != current.Count)
        {
            registered.RemoveWhere(channel =>
            {
                if (current.Contains(channel))
                    return false;
                channel.AccessorNodes.RemoveAccessorNode(accessor);
                return true;
            });
        }
    }

    public void Dispose()
    {
        foreach (var channel in registered)
            channel.AccessorNodes.RemoveAccessorNode(accessor);
        registered.Clear();
        current.Clear();
    }
}

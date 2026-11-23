using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VL.Core.EditorAttributes;
using VL.Core.Import;
using VL.Lib.Reactive;

namespace VL.Core.Reactive;

// Defined in VL.Core so it's easy to use in C#
public sealed class ChannelChoiceProvider : IChoiceProvider
{
    public async Task<IReadOnlyList<ChoiceEntry>> GetChoicesAsync(IChoiceContext context, CancellationToken cancellationToken)
    {
        return await context.ReadInstanceAsync(_ =>
        {
            var appHost = AppHost.Current;
            var channelHub = appHost.Services.GetService<IChannelHub>();
            if (channelHub is null)
                return ImmutableArray<ChoiceEntry>.Empty;
            
            var typeRegistry = context.TypeRegistry;
            var choices = ImmutableArray.CreateBuilder<ChoiceEntry>();
            foreach (var channel in SortByPath(channelHub.Channels.Values))
            {
                var key = channel.Path;
                var type = channel.ClrTypeOfValues;
                var typeInfo = typeRegistry.GetTypeInfo(type);
                choices.Add(new ChoiceEntry(
                    Value: key, 
                    Label: $"{key}{ChoiceEntry.LabelSeparator}{typeInfo.Name}", 
                    TypeArguments: [type]));
            }
            return choices.ToImmutable();
        });
    }

    // An order, like the one of a property, only ranks a channel among its siblings: the ones with the same parent path.
    // So the channels are grouped by their path up to the given depth and each group is ranked by the order of its own channel, a group without one has none.
    // The channel of a group comes before the ones below it.
    static IEnumerable<IChannel<object>> SortByPath(IEnumerable<IChannel<object>> channels, int depth = 0)
        => channels
            .GroupBy(channel => string.Join('.', channel.Path.Split('.').Take(depth + 1)))
            .OrderBy(group => group.FirstOrDefault(channel => channel.Path == group.Key)?.GetOrder().ValueOrDefault() ?? 0)
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => group.Where(channel => channel.Path == group.Key)
                .Concat(SortByPath(group.Where(channel => channel.Path != group.Key), depth + 1)));
}

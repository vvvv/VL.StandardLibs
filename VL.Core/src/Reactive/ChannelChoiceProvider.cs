using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VL.Core.Import;

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
            foreach (var channel in channelHub.Channels)
            {
                var key = channel.Key;
                var type = channel.Value.ClrTypeOfValues;
                var typeInfo = typeRegistry.GetTypeInfo(type);
                choices.Add(new ChoiceEntry(
                    Value: key, 
                    Label: $"{key}{ChoiceEntry.LabelSeparator}{typeInfo.Name}", 
                    TypeArguments: [type]));
            }
            return choices.ToImmutable();
        });
    }
}

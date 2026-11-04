#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VL.Core.Import;

namespace VL.Core;

/// <summary>
/// Offers just the names of the properties of the object (not a whole <see cref="ValuePath"/> like <see cref="ValuePathChoiceProvider"/>) linked to the input of the pin it is on, along with their type for the type argument of the node.
/// </summary>
[ChoiceProviderRequires("Input")]
public sealed class PropertyNameChoiceProvider : IChoiceProvider
{
    /// <summary>
    /// How long to wait for the node to run before giving up.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);

    public async Task<IReadOnlyList<ChoiceEntry>?> GetChoicesAsync(IChoiceContext context, CancellationToken cancellationToken)
    {
        if (!context.IsRunning)
            return null;

        var typeRegistry = context.TypeRegistry;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            // Only the type of the value is needed, the list is immutable
            return await context.ReadInputAsync("Input", input => GetChoices(typeRegistry, input), timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The node didn't run
            return null;
        }
    }

    static IReadOnlyList<ChoiceEntry>? GetChoices(TypeRegistry typeRegistry, object? input)
    {
        if (input is null)
            return null;

        var typeInfo = input is IVLObject obj ? obj.Type : typeRegistry.GetTypeInfo(input.GetType());
        return typeInfo.Properties
            .OrderBy(p => p.OriginalName)
            .Select(p => new ChoiceEntry(p.OriginalName, $"{p.OriginalName}{ChoiceEntry.LabelSeparator}{p.Type.ToString(includeCategory: false)}", [p.Type.ClrType]))
            .ToList();
    }
}

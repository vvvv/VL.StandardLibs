#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VL.Core.Import;
using VL.Lib.Reactive;

namespace VL.Core;

/// <summary>
/// Helps with building a <see cref="ValuePath"/> into the object linked to an input pin: the editor offers the segments that can follow what the pin says so far
/// and keeps offering them after each pick, until a choice ends the path. Picking sets the type argument of the node to the type the path points to.
/// Where the value ends (null, an empty collection) the choices continue with the declared types and are marked as <see cref="ChoiceEntry.TypeOnly"/>.
/// The object is taken from the pin named "Input", derive from this class to use another pin (and say so with <see cref="ChoiceProviderRequiresAttribute"/>).
/// If the pin links to a channel, the path starts at the object of the channel.
/// </summary>
[ChoiceProviderRequires(DefaultRootPin)]
public class ValuePathChoiceProvider : IChoiceProvider
{
    /// <summary>
    /// The pin the path starts at, unless a derived class says otherwise.
    /// </summary>
    public const string DefaultRootPin = "Input";

    /// <summary>
    /// The label of the choice that goes back one segment.
    /// </summary>
    public const string UpLabel = "..";

    readonly string rootPin;

    public ValuePathChoiceProvider() : this(DefaultRootPin)
    {
    }

    /// <param name="rootPin">The name of the pin with the object the path starts at.</param>
    protected ValuePathChoiceProvider(string rootPin)
    {
        this.rootPin = rootPin;
    }

    /// <summary>
    /// How long to wait for the node to run before giving up.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);

    public async Task<IReadOnlyList<ChoiceEntry>?> GetChoicesAsync(IChoiceContext context, CancellationToken cancellationToken)
    {
        if (!context.IsRunning)
            return null;

        var typeRegistry = context.TypeRegistry;
        var text = context.Text;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            // Walks the value at the moment it is traced, the entries are immutable
            return await context.ReadInputAsync(rootPin, input => GetChoices(typeRegistry, input, text), timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The node didn't run
            return null;
        }
    }

    /// <summary>
    /// Gets the choices for the text of the pin.
    /// </summary>
    /// <param name="typeRegistry">To look at VL types.</param>
    /// <param name="input">The value of the root pin. A channel is replaced by its object.</param>
    /// <param name="text">What the pin says so far. What can't be understood is ignored, as is a segment that doesn't lead anywhere.</param>
    /// <param name="maxItems">How many items of a collection to offer.</param>
    /// <returns>The choices or null if there's nothing to go by.</returns>
    public static IReadOnlyList<ChoiceEntry>? GetChoices(TypeRegistry? typeRegistry, object? input, string? text, int maxItems = ValuePathResolver.DefaultMaxItems)
    {
        var rootType = input?.GetType();
        if (input is IChannel channel)
        {
            rootType = channel.ClrTypeOfValues;
            input = channel.Object;
        }

        if (rootType is null)
            return null;

        var path = ValuePath.Parse(text);
        var result = ValuePathResolver.GetChildren(input, rootType, path, typeRegistry, maxItems);
        var depth = result.ResolvedSegmentCount;

        // The end of the path has nothing to pick, offer its siblings
        if (result.Children.Count == 0 && depth > 0)
        {
            depth--;
            result = ValuePathResolver.GetChildren(input, rootType, ValuePath.Create(path.Take(depth)), typeRegistry, maxItems);
        }

        var prefix = path.Take(depth).ToList();
        var entries = new List<ChoiceEntry>();
        if (prefix.Count > 0)
        {
            entries.Add(new ChoiceEntry(ValuePath.Format(prefix), Label(typeRegistry, ValuePath.Format(prefix), result.Type), [result.Type]));
            entries.Add(new ChoiceEntry(ValuePath.Format(prefix.SkipLast(1)), UpLabel, Continues: true));
        }

        foreach (var child in result.Children)
        {
            var value = ValuePath.Format(prefix.Append(child.Segment));
            entries.Add(new ChoiceEntry(value, Label(typeRegistry, child.Segment.ToString(), child.Type), [child.Type], child.HasChildren, child.IsTypeOnly));
        }

        if (result.OmittedCount > 0)
            entries.Add(new ChoiceEntry(null, $"... {result.OmittedCount} more, type the index"));

        return entries;
    }

    static string Label(TypeRegistry? typeRegistry, string name, Type? type)
        => type is null ? name : $"{name}{ChoiceEntry.LabelSeparator}{typeRegistry?.GetTypeInfo(type).ToString(includeCategory: false) ?? type.Name}";
}

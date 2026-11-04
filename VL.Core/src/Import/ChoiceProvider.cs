#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VL.Core.Import;

/// <summary>
/// Use on an input pin to let the editor offer a list of valid values for it. The list comes from the given <see cref="IChoiceProvider"/>,
/// which the editor instantiates (it needs a public parameterless constructor). The pin can still be set to other values, so the node must validate its input.
/// If the provider also implements <see cref="IChoiceNotifier"/> the list gets refreshed when the provider says so.
/// A pin of a patch that is connected directly to a pin with this attribute inherits it, as long as the patch offers the inputs the provider asks for with <see cref="ChoiceProviderRequiresAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class ChoiceProviderAttribute(Type providerType) : Attribute
{
    /// <summary>
    /// The type implementing <see cref="IChoiceProvider"/>.
    /// </summary>
    public Type ProviderType { get; } = providerType;

    /// <summary>
    /// Names the pins (inputs or outputs) whose types show the type of the picked choice, for a node with type parameters that the choice shall fill.
    /// The choice's <see cref="ChoiceEntry.TypeArguments"/> go to the type parameters occurring in the type of the first pin, then to those only occurring in the second and so on, each in the order the node declares them.
    /// Needed if more than one type parameter is free (the node has several pins with a choice provider, each one for another type parameter),
    /// or if the type parameter to fill is not the only one that the inputs read by the provider leave open.
    /// Without it the choice fills the type parameters that the inputs named with <see cref="ChoiceProviderRequiresAttribute"/> don't determine.
    /// A patch inheriting the provider has to offer these pins too.
    /// </summary>
    public string[] TypePins { get; set; } = [];
}

/// <summary>
/// Use on an <see cref="IChoiceProvider"/> to name the input pins it reads with <see cref="IChoiceContext.ReadInputAsync{T}"/>.
/// The provider always works on the node the user edits, so a patch wrapping a node with a choice provider can only pass the provider on
/// if it offers these inputs itself (matched by name, ignoring case). The pin the provider is on doesn't need to be listed.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class ChoiceProviderRequiresAttribute(params string[] inputPins) : Attribute
{
    /// <summary>
    /// The names of the inputs.
    /// </summary>
    public IReadOnlyList<string> InputPins { get; } = inputPins;

    /// <summary>
    /// Gets the names of the inputs the given provider type needs.
    /// </summary>
    public static IReadOnlyList<string> GetInputPins(Type providerType)
        => ((ChoiceProviderRequiresAttribute?)GetCustomAttribute(providerType, typeof(ChoiceProviderRequiresAttribute)))?.InputPins ?? [];
}

/// <summary>
/// Provides the choices for a pin marked with <see cref="ChoiceProviderAttribute"/>.
/// </summary>
public interface IChoiceProvider
{
    /// <summary>
    /// Gets the choices. Gets called on the UI thread, so don't block. Continuations may run on any thread, so don't touch UI state.
    /// </summary>
    /// <param name="context">Gives async access to the node the choices are for.</param>
    /// <param name="cancellationToken">Gets cancelled when the choices aren't needed anymore.</param>
    /// <returns>The choices or null if the provider can't offer any in the current situation, for example because the node isn't running. The editor then falls back to a plain input for the value.</returns>
    Task<IReadOnlyList<ChoiceEntry>?> GetChoicesAsync(IChoiceContext context, CancellationToken cancellationToken);
}

/// <summary>
/// To be implemented by an <see cref="IChoiceProvider"/> whose choices change over time.
/// </summary>
public interface IChoiceNotifier
{
    /// <summary>
    /// Fires when the choices have changed. The editor then asks the provider again.
    /// </summary>
    IObservable<object?> Changed { get; }
}

/// <summary>
/// Gives an <see cref="IChoiceProvider"/> access to the node the choices are for.
/// The data lives on the thread of the runtime and is only safe to touch at the very moment it gets traced: the runtime can change it right after
/// or hand out data that is only valid during the call. Therefore the context doesn't hand out values, it runs a delegate to read them at that moment.
/// The delegate must return an immutable representation of what it needs, the task completes with it.
/// A call completes only once the code producing the data ran, so use the cancellation token or a timeout if the provider doesn't want to wait for that.
/// </summary>
public interface IChoiceContext
{
    /// <summary>
    /// The text of the pin the choices are for, as it is right now. Lets a provider offer choices that build on what's been picked before, see <see cref="ChoiceEntry.Continues"/>.
    /// Null if the context doesn't know it.
    /// </summary>
    string? Text => null;

    /// <summary>
    /// The type registry of the editor, to look at the types of values. Types of values coming from the running program are known to it.
    /// </summary>
    TypeRegistry TypeRegistry { get; }

    /// <summary>
    /// Whether the user runtime (the one running the user's app, not the one of the editor, which is always on) is running. If not, nothing gets traced and a read of the live value would only wait, so a provider can return early
    /// (null, or choices that don't need a value). Defaults to true for contexts that don't know. Being true doesn't guarantee the node executes: it might sit in a patch that isn't called.
    /// </summary>
    bool IsRunning => true;

    /// <summary>
    /// The path of the document the choices are for, that is the one the user edits. Null if it is unknown or the document has no file yet.
    /// </summary>
    string? DocumentPath => null;

    /// <summary>
    /// Runs <paramref name="read"/> synchronously on the thread of the runtime, at the very moment the process instance of the node gets traced.
    /// Don't block in <paramref name="read"/> and return an immutable representation of what you need.
    /// For nodes that aren't processes <paramref name="read"/> is called with null on the calling thread.
    /// </summary>
    Task<T> ReadInstanceAsync<T>(Func<object?, T> read, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="read"/> synchronously on the thread of the runtime, at the very moment the value of the given input pin gets traced.
    /// Don't block in <paramref name="read"/> and return an immutable representation of what you need.
    /// The pin is matched by name, ignoring case. The value of a linked pin is the one its source produced, so it is available even if the node itself isn't running yet.
    /// Throws an <see cref="ArgumentException"/> if the node has no such input.
    /// </summary>
    Task<T> ReadInputAsync<T>(string pinName, Func<object?, T> read, CancellationToken cancellationToken = default);
}

/// <summary>
/// A choice offered by an <see cref="IChoiceProvider"/>.
/// </summary>
/// <param name="Value">The value for the pin, null if the choice only sets type arguments.</param>
/// <param name="Label">The text shown for the choice, defaults to the value. A <see cref="ChoiceEntry.LabelSeparator"/> splits it into the main text and a secondary one, like the type behind a name, which the editor shows less prominent.
/// Only the value gets inserted.</param>
/// <param name="TypeArguments">The type arguments to set on the node reference when the choice gets picked, for those type parameters of the node that the inputs named with <see cref="ChoiceProviderRequiresAttribute"/>
/// don't determine (those get inferred from the data), in the order the node declares them. A node with a different number of such type parameters is left alone.
/// A null entry leaves the type argument open for type inference. Null leaves all type arguments as they are.</param>
/// <param name="Continues">The value is only a step on the way, as for a path of which the user picks one segment at a time. The editor writes the value, leaves the choices open
/// and asks the provider again, now with the new value as <see cref="IChoiceContext.Text"/>. A choice that doesn't continue ends the selection.</param>
/// <param name="TypeOnly">The editor shows that the choice is a guess: the provider derived it from types, it hasn't seen it in the data.</param>
public sealed record ChoiceEntry(string? Value, string? Label = null, IReadOnlyList<Type?>? TypeArguments = null, bool Continues = false, bool TypeOnly = false)
{
    /// <summary>
    /// Separates the main text of a <see cref="Label"/> from its secondary text.
    /// </summary>
    public const char LabelSeparator = '	';
}

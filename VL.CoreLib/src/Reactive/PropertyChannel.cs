#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Reactive;

[assembly: ImportType(typeof(PropertyChannel<>), Category = "Reactive.Channel")]

namespace VL.Lib.Reactive;

/// <summary>
/// Binds a channel to a property of an object, or to an item of a list or dictionary. Changes are pushed into the channel and values set on the channel are written back.
/// If the object implements INotifyPropertyChanged (INotifyCollectionChanged for items) it is read when it changes, otherwise it is read on every update.
/// </summary>
[ProcessNode]
public sealed class PropertyChannel<T> : IDisposable
{
    private readonly NodeContext nodeContext;
    private readonly IChannel<T> channel = Channel.Create<T>(default!);
    private readonly IDisposable channelSubscription;
    private readonly SerialDisposable warnings = new();
    private readonly SerialDisposable notifications = new();

    private object? input;
    private string? boundPath;
    private PropertyPathWatcher? watcher;
    private object? instance;
    private ValuePathSegment? segment;
    private string? shownProblem;
    private Slot? slot;
    private bool canWrite;
    private bool isNotifying;
    private bool isReading;
    private bool hasLastSetValue;
    private T? lastSetValue;

    public PropertyChannel([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    {
        this.nodeContext = nodeContext;
        channelSubscription = channel.Subscribe(Write);
    }

    /// <summary>
    /// Binds the channel to a property of the given instance or of an object further down, as a <see cref="ValuePath"/> leads to it. The last segment of the path is the property,
    /// or an index or key if the channel is to be bound to an item of a collection. Rebinds when the object owning the property or item or the last segment changes.
    /// Objects on the way are listened to where they allow it (see <see cref="PropertyPathWatcher"/>), the path is only walked again when one of them reports a change or can't tell.
    /// </summary>
    /// <param name="input">The object the path starts at</param>
    /// <param name="path">The path to the property or item, a plain property name for a property of the input itself</param>
    /// <returns>The channel bound to the property or item</returns>
    [Fragment(Order = -1)]
    [return: Pin(Name = "Output")]
    public IChannel<T> Update(object? input, [NodeLabel, ChoiceProvider(typeof(ValuePathChoiceProvider))] string? path)
    {
        if (watcher is null || !ReferenceEquals(input, this.input) || path != boundPath)
        {
            this.input = input;
            boundPath = path;
            watcher?.Dispose();
            watcher = new PropertyPathWatcher(nodeContext.AppHost.TypeRegistry, input, path);
            Rebind(watcher.Owner, watcher.Last);
        }
        else if (watcher.Refresh())
            Rebind(watcher.Owner, watcher.Last);
        else if (slot != null && !isNotifying)
            Read();

        ShowPathProblem();
        return channel;
    }

    /// <summary>
    /// The value of the property. Setting it writes the property, but only when the value differs from the one set before.
    /// </summary>
    public T? Value
    {
        [Fragment(IsDefault = true)]
        get => channel.Value;
        set
        {
            if (hasLastSetValue && EqualityComparer<T>.Default.Equals(value, lastSetValue))
                return;

            hasLastSetValue = true;
            lastSetValue = value;
            channel.Value = value;
        }
    }

    public void Dispose()
    {
        Unbind();
        watcher?.Dispose();
        channelSubscription.Dispose();
        channel.Dispose();
        warnings.Dispose();
        notifications.Dispose();
    }

    private void Rebind(object? instance, ValuePathSegment? segment)
    {
        Unbind();

        this.instance = instance;
        this.segment = segment;
        hasLastSetValue = false;

        if (instance is null || segment is not { } last)
            return;

        var isProperty = last.Kind == ValuePathSegmentKind.Property;
        var newSlot = isProperty ? GetPropertySlot(instance, last.Name!) : GetItemSlot(instance, last);
        if (newSlot is null)
            return;

        if (!typeof(T).IsAssignableFrom(newSlot.Type))
        {
            Warn($"The {newSlot.Description} is of type {nodeContext.AppHost.TypeRegistry.GetTypeInfo(newSlot.Type)} and can't be read as {typeof(T).Name}.");
            return;
        }

        slot = newSlot;
        canWrite = newSlot.Type.IsAssignableFrom(typeof(T));

        if (isProperty && instance is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += Instance_PropertyChanged;
            notifications.Disposable = Disposable.Create(() => notifyPropertyChanged.PropertyChanged -= Instance_PropertyChanged);
            isNotifying = true;
        }
        else if (!isProperty && instance is INotifyCollectionChanged notifyCollectionChanged)
        {
            NotifyCollectionChangedEventHandler handler = (_, _) => Read(force: true);
            notifyCollectionChanged.CollectionChanged += handler;
            notifications.Disposable = Disposable.Create(() => notifyCollectionChanged.CollectionChanged -= handler);
            isNotifying = true;
        }

        Read();
    }

    private Slot? GetPropertySlot(object instance, string name)
    {
        var typeInfo = instance is IVLObject obj ? obj.Type : nodeContext.AppHost.TypeRegistry.GetTypeInfo(instance.GetType());
        var property = typeInfo.GetProperty(name);
        if (property is null)
        {
            Warn($"{typeInfo} has no property {name}.");
            return null;
        }

        return new Slot($"property {name}", property.Type.ClrType, owner => (true, property.GetValue(owner)), property.WithValue);
    }

    private Slot? GetItemSlot(object instance, ValuePathSegment item)
    {
        var itemType = ValuePathResolver.GetSegmentType(instance, item, nodeContext.AppHost.TypeRegistry);
        if (itemType is null)
        {
            Warn($"{instance.GetType().Name} has no items to pick {item} from.");
            return null;
        }

        var itemPath = item.AppendTo(string.Empty);
        return new Slot($"item {item}", itemType,
            owner =>
            {
                owner.TryGetValueByPath<object?>(itemPath, null, out var value, out var exists);
                return (exists, value);
            },
            (owner, value) => owner.WithValueByPath(itemPath, value, out _));
    }

    // A path that doesn't exist is something the user has to fix, unlike a null on the way, which is just a value
    private void ShowPathProblem()
    {
        var problem = watcher?.Problem;
        if (problem == shownProblem)
            return;

        shownProblem = problem;
        if (problem is null)
            warnings.Disposable = null;
        else
            Warn(problem);
    }

    private void Unbind()
    {
        shownProblem = null;
        notifications.Disposable = null;
        isNotifying = false;
        slot = null;
        warnings.Disposable = null;
    }

    private void Instance_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == segment?.Name)
            Read(force: true);
    }

    private void Read(bool force = false)
    {
        if (slot is null || instance is null)
            return;

        var value = slot.Read(instance).Value is T t ? t : default;
        var author = segment?.ToString();

        isReading = true;
        try
        {
            if (force)
                channel.SetValueAndAuthor(value, author: author);
            else
                channel.EnsureValue(value, author: author);
        }
        finally
        {
            isReading = false;
        }
    }

    private void Write(T? value)
    {
        if (isReading || slot is null || instance is null)
            return;

        if (!canWrite)
        {
            nodeContext.warnForAWhile(() => $"Values of type {typeof(T).Name} can't be written to the {slot.Description} of type {slot.Type}.");
            return;
        }

        if (!slot.Read(instance).Exists)
        {
            nodeContext.warnForAWhile(() => $"There is no {slot.Description} to write to.");
            return;
        }

        var result = slot.Write(instance, value);
        if (ReferenceEquals(result, instance))
            return;

        // The owner is immutable, so what holds it gets the new one
        if (watcher?.TryReplaceOwner(result) != true)
            nodeContext.warnForAWhile(() => $"The {slot.Description} can't be written, {instance.GetType().Name} is immutable and can't be replaced in what holds it.");
        else if (watcher.Refresh())
            Rebind(watcher.Owner, watcher.Last);
    }

    private void Warn(string warning)
    {
        var disposables = new CompositeDisposable();
        disposables.addWarnings(() => warning, nodeContext);
        warnings.Disposable = disposables;
    }

    /// <summary>
    /// What the channel is bound to, a property or an item of the owner.
    /// </summary>
    /// <param name="Description">Says what it is, for messages.</param>
    /// <param name="Type">The declared type.</param>
    /// <param name="Read">Reads it from the owner and tells if it exists: a property always does, an item might not.</param>
    /// <param name="Write">Gives the owner with the new value in it, which is another object if the owner is immutable.</param>
    private sealed record Slot(string Description, Type Type, Func<object, (bool Exists, object? Value)> Read, Func<object, object?, object?> Write);
}

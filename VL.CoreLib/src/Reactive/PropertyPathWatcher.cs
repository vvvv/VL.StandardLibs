#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using VL.Core;

namespace VL.Lib.Reactive;

/// <summary>
/// Follows a <see cref="ValuePath"/> into an object graph to the object owning the property the path ends with, and notices when the objects on the way may have been replaced.
/// For each object on the way it only does what is needed to learn about a change of the part the path takes from it:
/// <list type="bullet">
/// <item>Immutable objects can't change, a replacement shows up at the object holding them.</item>
/// <item>Objects implementing <see cref="INotifyPropertyChanged"/> (properties) or <see cref="INotifyCollectionChanged"/> (items) get listened to.</item>
/// <item>Only for the rest, <see cref="Refresh"/> has to walk the path again, which is cheap but not free.</item>
/// </list>
/// So a graph of notifying objects costs nothing as long as nothing happens. Notifications can arrive on any thread, they only set a flag that <see cref="Refresh"/> looks at.
/// The owner itself isn't listened to, this is up to the user of the watcher.
/// </summary>
sealed class PropertyPathWatcher : IDisposable
{
    readonly TypeRegistry typeRegistry;
    readonly object? root;
    readonly List<ValuePathSegment> hops = [];
    readonly SerialDisposable subscriptions = new();
    object?[] visited = [];
    volatile bool changed;
    bool needsPolling;

    /// <param name="typeRegistry">To find out if objects are immutable.</param>
    /// <param name="root">The object the path starts at.</param>
    /// <param name="text">The path, the last segment being the name of the property. Anything that isn't a path is taken as the name of a property of the root.</param>
    public PropertyPathWatcher(TypeRegistry typeRegistry, object? root, string? text)
    {
        this.typeRegistry = typeRegistry;
        this.root = root;
        Name = text;

        if (ValuePath.TryParse(text, out var parsed) && parsed.Count > 0)
        {
            var last = parsed[parsed.Count - 1];
            Name = last.Name ?? last.ToString();
            hops.AddRange(parsed.Take(parsed.Count - 1));
        }

        Walk();
    }

    /// <summary>
    /// The object owning the property, null if the path doesn't lead to one.
    /// </summary>
    public object? Owner { get; private set; }

    /// <summary>
    /// Says why the path doesn't lead to an object, if there is no such thing on the way. Null if it does, or if there is just a null on the way (which can change any time).
    /// </summary>
    public string? Problem { get; private set; }

    /// <summary>
    /// The name of the property of the <see cref="Owner"/>.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Whether <see cref="Refresh"/> has to look at the graph each time, because there are objects that can't tell when they change.
    /// </summary>
    public bool NeedsPolling => needsPolling;

    /// <summary>
    /// Looks at the graph if something may have changed. Returns true if the <see cref="Owner"/> is another one now.
    /// </summary>
    public bool Refresh()
    {
        if (!changed && !needsPolling)
            return false;

        changed = false;
        var previous = Owner;
        Walk();
        return !ReferenceEquals(previous, Owner);
    }

    public void Dispose() => subscriptions.Dispose();

    void Walk()
    {
        var objects = new List<object?> { root };
        var current = root;
        Problem = null;
        for (var i = 0; i < hops.Count && current is not null; i++)
        {
            current.TryGetValueByPath<object?>(hops[i].AppendTo(string.Empty), null, out var next, out var exists);
            if (!exists)
                Problem = $"The path doesn't exist: {current.GetType().Name} has nothing at {hops[i].AppendTo(string.Empty)}.";
            objects.Add(next);
            current = next;
        }

        Owner = current;

        // Only listen again if the objects on the way are other ones
        if (visited.Length == objects.Count && visited.Zip(objects).All(pair => ReferenceEquals(pair.First, pair.Second)))
            return;

        visited = objects.ToArray();
        needsPolling = false;
        var composite = new CompositeDisposable();
        // Every object that a segment is taken from, so not the owner
        for (var i = 0; i < hops.Count && i < objects.Count && objects[i] is { } holder; i++)
        {
            if (Watch(holder, hops[i]) is { } subscription)
                composite.Add(subscription);
        }
        subscriptions.Disposable = composite;
    }

    IDisposable? Watch(object holder, ValuePathSegment segment)
    {
        if (IsImmutable(holder))
            return null;

        if (segment.Kind == ValuePathSegmentKind.Property && holder is INotifyPropertyChanged notifyPropertyChanged)
        {
            PropertyChangedEventHandler handler = (_, e) =>
            {
                if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == segment.Name)
                    changed = true;
            };
            notifyPropertyChanged.PropertyChanged += handler;
            return Disposable.Create(() => notifyPropertyChanged.PropertyChanged -= handler);
        }

        if (segment.Kind != ValuePathSegmentKind.Property && holder is INotifyCollectionChanged notifyCollectionChanged)
        {
            NotifyCollectionChangedEventHandler handler = (_, _) => changed = true;
            notifyCollectionChanged.CollectionChanged += handler;
            return Disposable.Create(() => notifyCollectionChanged.CollectionChanged -= handler);
        }

        needsPolling = true;
        return null;
    }

    bool IsImmutable(object value)
    {
        var type = value.GetType();
        if (type.IsValueType || value is string)
            return true;

        return (value is IVLObject obj ? obj.Type : typeRegistry.GetTypeInfo(type)).IsImmutable;
    }
}

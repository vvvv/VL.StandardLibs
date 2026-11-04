#nullable enable
using System;
using System.Collections.Generic;
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
/// Binds a channel to a property of an object. Changes of the property are pushed into the channel and values set on the channel are written to the property.
/// If the object implements INotifyPropertyChanged the property is read when it changes, otherwise it is read on every update.
/// </summary>
[ProcessNode]
public sealed class PropertyChannel<T> : IDisposable
{
    private readonly NodeContext nodeContext;
    private readonly IChannel<T> channel = Channel.Create<T>(default!);
    private readonly IDisposable channelSubscription;
    private readonly SerialDisposable warnings = new();

    private object? input;
    private string? path;
    private PropertyPathWatcher? watcher;
    private object? instance;
    private string? propertyName;
    private string? shownProblem;
    private IVLPropertyInfo? property;
    private bool canWrite;
    private bool isReading;
    private bool hasLastSetValue;
    private T? lastSetValue;

    public PropertyChannel([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    {
        this.nodeContext = nodeContext;
        channelSubscription = channel.Subscribe(Write);
    }

    /// <summary>
    /// Binds the channel to a property of the given instance or of an object further down, as a <see cref="ValuePath"/> leads to it. The last segment of the path must be the property.
    /// Rebinds when the object owning the property or the property name changes. Objects on the way are listened to where they allow it (see <see cref="PropertyPathWatcher"/>), the path is only walked again when one of them reports a change or can't tell.
    /// </summary>
    /// <param name="input">The object the path starts at</param>
    /// <param name="propertyName">The path to the property, a plain property name for a property of the input itself</param>
    /// <returns>The channel bound to the property</returns>
    [Fragment(Order = -1)]
    [return: Pin(Name = "Output")]
    public IChannel<T> Update(object? input, [NodeLabel, ChoiceProvider(typeof(ValuePathChoiceProvider))] string? propertyName)
    {
        if (watcher is null || !ReferenceEquals(input, this.input) || propertyName != path)
        {
            this.input = input;
            path = propertyName;
            watcher?.Dispose();
            watcher = new PropertyPathWatcher(nodeContext.AppHost.TypeRegistry, input, propertyName);
            Rebind(watcher.Owner, watcher.Name);
        }
        else if (watcher.Refresh())
            Rebind(watcher.Owner, watcher.Name);
        else if (property != null && instance is not INotifyPropertyChanged)
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
    }

    private void Rebind(object? instance, string? propertyName)
    {
        Unbind();

        this.instance = instance;
        this.propertyName = propertyName;
        hasLastSetValue = false;

        if (instance is null || string.IsNullOrEmpty(propertyName))
            return;

        var typeInfo = instance is IVLObject obj ? obj.Type : nodeContext.AppHost.TypeRegistry.GetTypeInfo(instance.GetType());
        var property = typeInfo.GetProperty(propertyName);
        if (property is null)
        {
            Warn($"{typeInfo} has no property {propertyName}.");
            return;
        }

        var propertyType = property.Type.ClrType;
        if (!typeof(T).IsAssignableFrom(propertyType))
        {
            Warn($"The property {propertyName} is of type {property.Type} and can't be read as {typeof(T).Name}.");
            return;
        }

        this.property = property;
        canWrite = propertyType.IsAssignableFrom(typeof(T));

        if (instance is INotifyPropertyChanged notifyPropertyChanged)
            notifyPropertyChanged.PropertyChanged += Instance_PropertyChanged;

        Read();
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
        if (instance is INotifyPropertyChanged notifyPropertyChanged)
            notifyPropertyChanged.PropertyChanged -= Instance_PropertyChanged;

        property = null;
        warnings.Disposable = null;
    }

    private void Instance_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == propertyName)
            Read(force: true);
    }

    private void Read(bool force = false)
    {
        if (property is null || instance is null)
            return;

        var value = property.GetValue(instance) is T t ? t : default;

        isReading = true;
        try
        {
            if (force)
                channel.SetValueAndAuthor(value, author: propertyName);
            else
                channel.EnsureValue(value, author: propertyName);
        }
        finally
        {
            isReading = false;
        }
    }

    private void Write(T? value)
    {
        if (isReading || property is null || instance is null)
            return;

        if (!canWrite)
        {
            nodeContext.warnForAWhile(() => $"Values of type {typeof(T).Name} can't be written to the property {propertyName} of type {property.Type}.");
            return;
        }

        var result = property.WithValue(instance, value);
        if (!ReferenceEquals(result, instance))
            nodeContext.warnForAWhile(() => $"The property {propertyName} can't be written, {property.DeclaringType} is immutable.");
    }

    private void Warn(string warning)
    {
        var disposables = new CompositeDisposable();
        disposables.addWarnings(() => warning, nodeContext);
        warnings.Disposable = disposables;
    }
}

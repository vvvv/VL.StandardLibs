#nullable enable
using System;
using System.ComponentModel;
using System.Threading;
using VL.Core.Import;
using VL.Lib.Control;

[assembly: ImportType(typeof(PropertyChanged))]

namespace VL.Lib.Control;

[ProcessNode]
public sealed class PropertyChanged : IDisposable
{
    private INotifyPropertyChanged? instance;
    private string? propertyName;
    private int changeCounter;

    public PropertyChanged([Pin(Visibility = Model.PinVisibility.Optional)] bool changedOnCreate)
    {
        changeCounter = changedOnCreate ? 1 : 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="instance">The </param>
    /// <param name="propertyName">Leave empty to listen for changes on all properties</param>
    /// <param name="result">Whether or not a property changed</param>
    /// <param name="unchanged">Whether or not all properties stayed the same</param>
    public void Update(INotifyPropertyChanged? instance, string? propertyName, out bool result, out bool unchanged)
    {
        if (this.instance != instance)
        {
            Resubscribe(instance);
        }

        this.propertyName = propertyName;

        var counter = Interlocked.Exchange(ref changeCounter, 0);
        if (counter > 0)
        {
            result = true;
            unchanged = false;
        }
        else
        {
            result = false;
            unchanged = true;
        }
    }

    private void Resubscribe(INotifyPropertyChanged? instance)
    {
        if (this.instance != null)
            this.instance.PropertyChanged -= Instance_PropertyChanged;

        this.instance = instance;
        Interlocked.Increment(ref changeCounter);

        if (this.instance != null)
            this.instance.PropertyChanged += Instance_PropertyChanged;
    }

    public void Dispose()
    {
        if (instance != null)
            instance.PropertyChanged -= Instance_PropertyChanged;
    }

    private void Instance_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(propertyName) || e.PropertyName == propertyName)
        {
            Interlocked.Increment(ref changeCounter);
        }
    }
}

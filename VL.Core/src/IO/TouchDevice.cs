using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using VL.Lib.IO.Notifications;

namespace VL.Lib.IO
{
    public interface ITouchDevice
    {
        IObservable<TouchNotification> Notifications { get; }
    }

    public class TouchDevice : ITouchDevice
    {
        public static readonly TouchDevice Empty = new TouchDevice(Observable.Never<TouchNotification>());

        public readonly IObservable<TouchNotification> Notifications;

        public TouchDevice(IObservable<TouchNotification> notifications)
        {
            Notifications = notifications
                .Publish()
                .RefCount();
        }

        IObservable<TouchNotification> ITouchDevice.Notifications => Notifications;
    }
}

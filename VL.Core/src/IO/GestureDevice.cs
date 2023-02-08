using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using VL.Lib.IO.Notifications;

namespace VL.Lib.IO
{
    public interface IGestureDevice
    {
        IObservable<GestureNotification> Notifications { get; }
    }

    public class GestureDevice : IGestureDevice
    {
        public static readonly GestureDevice Empty = new GestureDevice(Observable.Never<GestureNotification>());

        public readonly IObservable<GestureNotification> Notifications;

        public GestureDevice(IObservable<GestureNotification> notifications)
        {
            Notifications = notifications
                .Publish()
                .RefCount();
        }

        IObservable<GestureNotification> IGestureDevice.Notifications => Notifications;
    }
}

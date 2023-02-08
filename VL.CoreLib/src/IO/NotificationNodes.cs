using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VL.Lib.Collections;
using VL.Lib.IO.Notifications;

namespace VL.Lib.IO
{
    public class ToMouse : IMouse
    {
        Subject<MouseNotification> m = new Subject<MouseNotification>();
        public IObservable<MouseNotification> Notifications => m;

        IDisposable subscription;

        IObservable<object> input;
        IObservable<object> Input
        {
            get { return input; }
            set
            {
                if (value != input)
                {
                    if (subscription != null)
                    {
                        subscription.Dispose();
                        subscription = null;
                    }

                    input = value;

                    if (value != null)
                        subscription = input.Subscribe(n => Notify(n));
                }
            }
        }

        private MouseButtons FPressedButtons;
        public MouseButtons PressedButtons => FPressedButtons;

        void Notify(object notification)
        {
            var mn = notification as MouseNotification;

            if (mn != null)
            {
                switch (mn.Kind)
                {
                    case MouseNotificationKind.MouseDown:
                        var mouseDown = mn as MouseDownNotification;
                        FPressedButtons |= mouseDown.Buttons;
                        break;
                    case MouseNotificationKind.MouseUp:
                        var mouseUp = mn as MouseUpNotification;
                        FPressedButtons &= ~mouseUp.Buttons;
                        break;
                    default:
                        break;
                }

                m.OnNext(mn);
            }
            //else
            //{
            //    var kn = notification as KeyNotification;
            //    if (kn != null)
            //        k.OnNext(kn);
            //}
        }

        public void Update(IObservable<object> notifications, out IMouse output)
        {
            Input = notifications;
            output = this;
        }
    }


    public class SpliceNotifications
    {
        Dictionary<object, Subject<INotification>> D = new Dictionary<object, Subject<INotification>>();

        IDisposable subscription;

        IObservable<INotification> input;
        IObservable<INotification> Input
        {
            get { return input; }
            set
            {
                if (value != input)
                {
                    if (subscription != null)
                    {
                        subscription.Dispose();
                        subscription = null;
                    }

                    input = value;

                    if (value != null)
                        subscription = input.Subscribe(n => Notify(n));
                }
            }
        }

        public void Notify(INotification notification)
        {
            var channel = D.EnsureValue(notification.Sender, s => new Subject<INotification>());
            channel.OnNext(notification);
        }

        public void Update(IObservable<INotification> input,
            out IEnumerable<IObservable<INotification>> output,
            out Spread<object> senders)
        {
            Input = input;
            output = D.Values.ToSpread();
            senders = D.Keys.ToSpread();
        }
    }
}

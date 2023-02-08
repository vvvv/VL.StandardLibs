using System;
using System.Reactive.Subjects;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;

namespace VL.Skia
{
    public class Console : LinkedLayerBase, IMouse, IKeyboard, ITouchDevice
    {
        Subject<MouseNotification> m = new Subject<MouseNotification>();
        IObservable<MouseNotification> IMouse.Notifications => m;

        Subject<KeyNotification> k = new Subject<KeyNotification>();
        IObservable<KeyNotification> IKeyboard.Notifications => k;

        Subject<TouchNotification> t = new Subject<TouchNotification>();
        IObservable<TouchNotification> ITouchDevice.Notifications => t;

        Subject<INotification> all = new Subject<INotification>();
        public IObservable<INotification> Notifications => all;

        public MouseButtons PressedButtons
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Keys Modifiers
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool CapsLock
        {
            get
            {
                throw new NotImplementedException();
            }
        }


        bool Block, Force, TeatAsProcessed;

        public override bool Notify(INotification notification, CallerInfo caller)
        {
            var processed = false;
            if (!Block)
                processed = base.Notify(notification, caller);
            if (Force || !processed)
            {
                if (notification is MouseNotification mn)
                {
                    m.OnNext(mn);
                    all.OnNext(mn);
                }
                else if (notification is KeyNotification kn)
                {
                    k.OnNext(kn);
                    all.OnNext(kn);
                }
                else if (notification is TouchNotification tn)
                {
                    t.OnNext(tn);
                    all.OnNext(tn);
                }
            }
            return TeatAsProcessed;
        }

        public void Update(ILayer input, bool block, bool force, bool treatAsProcessed,
            out ILayer output, out IMouse mouse, out IKeyboard keyboard, out ITouchDevice touchDevice, out IObservable<INotification> notifications)
        {
            Input = input;
            Block = block;
            Force = force;
            TeatAsProcessed = treatAsProcessed;
            output = this;
            mouse = this;
            keyboard = this;
            touchDevice = this;
            notifications = Notifications;
        }
    }
}

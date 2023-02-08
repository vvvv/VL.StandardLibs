using System;
using System.Linq;
using System.Reactive.Linq;
using VL.Lib.IO.Notifications;

namespace VL.Lib.IO
{
    public interface IKeyboard
    {
        IObservable<KeyNotification> Notifications { get; }
        Keys Modifiers { get; }
        bool CapsLock { get; }
    }

    public class Keyboard : IKeyboard // , IDisposable
    {
        public static readonly Keyboard Empty = new Keyboard(Observable.Never<KeyNotification>());

        public readonly IObservable<KeyNotification> Notifications;

        IObservable<KeyNotification> IKeyboard.Notifications => Notifications;

        //private IDisposable FSubscription;
        private readonly byte[] FKeyStates = new byte[256];
        const byte KEY_PRESSED = 0x80;
        const byte KEY_TOGGLED = 0x01;

        public Keyboard(IObservable<KeyNotification> keyNotifications)
        {
            keyNotifications = keyNotifications
                .Do(n =>
                {
                    Keys keyCode;
                    int i;
                    switch (n.Kind)
                    {
                        case KeyNotificationKind.KeyDown:
                            keyCode = ((KeyCodeNotification)n).KeyCode;
                            i = (int)keyCode;
                            if (i < FKeyStates.Length)
                            {
                                FKeyStates[i] = keyCode == Keys.NumLock || keyCode == Keys.CapsLock
                                    ? (byte)(FKeyStates[i] ^ KEY_TOGGLED)
                                    : KEY_PRESSED;
                            }
                            break;
                        case KeyNotificationKind.KeyUp:
                            keyCode = ((KeyCodeNotification)n).KeyCode;
                            i = (int)keyCode;
                            if (i < FKeyStates.Length)
                            {
                                FKeyStates[i] = keyCode == Keys.NumLock || keyCode == Keys.CapsLock
                                    ? (byte)FKeyStates[i]
                                    : (byte)0;
                            }
                            break;
                    }
                }
            );
            Notifications = keyNotifications.Publish().RefCount();
            //Subscribe();
        }

        public Keys Modifiers
        {
            get
            {
                var modifiers = Keys.None;
                if (FKeyStates[(int)Keys.ControlKey] == KEY_PRESSED)
                    modifiers |= Keys.Control;
                if (FKeyStates[(int)Keys.Menu] == KEY_PRESSED)
                    modifiers |= Keys.Alt;
                if (FKeyStates[(int)Keys.ShiftKey] == KEY_PRESSED)
                    modifiers |= Keys.Shift;
                return modifiers;
            }
        }

        public bool CapsLock { get; set; }

        //public void Dispose()
        //{
        //    if (FSubscription != null)
        //    {
        //        FSubscription.Dispose();
        //        FSubscription = null;
        //    }
        //}

        //private KeyboardState FState;
        //public KeyboardState State
        //{
        //    get
        //    {
        //        if (FState == null)
        //            Subscribe();
        //        return FState;
        //    }
        //}

        //private void Subscribe()
        //{
        //    FState = new KeyboardState();
        //    FSubscription = KeyNotifications.Subscribe(notification =>
        //        {
        //            var capsLock = Control.IsKeyLocked(Keys.CapsLock);
        //            var chars = Enumerable.Empty<char>();
        //            var keys = Enumerable.Empty<Keys>();
        //            switch (notification.Kind)
        //            {
        //                case KeyNotificationKind.KeyDown:
        //                    var keyDown = notification as KeyDownNotification;
        //                    keys = FState.KeyCodes.Concat(new[] { keyDown.Value.KeyCode });
        //                    break;
        //                case KeyNotificationKind.KeyPress:
        //                    var keyPress = notification as KeyPressNotification;
        //                    chars = new[] { keyPress.Value.KeyChar };
        //                    keys = State.KeyCodes;
        //                    break;
        //                case KeyNotificationKind.KeyUp:
        //                    var keyUp = notification as KeyUpNotification;
        //                    keys = State.KeyCodes.Except(new[] { keyUp.Value.KeyCode });
        //                    break;
        //            }
        //            // TODO: Find way to retrieve time
        //            FState = new KeyboardState(keys, chars, capsLock, 0);
        //        }
        //    );
        //}
    }
}

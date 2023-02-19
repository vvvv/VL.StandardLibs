namespace VL.Lib.IO.Notifications
{
    public abstract class NotificationBase : INotification
    {
        public readonly Keys ModifierKeys;
        public readonly object Sender;

        public NotificationBase(object sender, Keys modifierKeys)
        {
            Sender = sender;
            ModifierKeys = modifierKeys;
        }

        public bool Handled { get; set; }

        public bool AltKey => (ModifierKeys & Keys.Alt) != 0;
        public bool ShiftKey => (ModifierKeys & Keys.Shift) != 0;
        public bool CtrlKey => (ModifierKeys & Keys.Control) != 0;

        object INotification.Sender => Sender;

        public abstract INotification WithSender(object sender);
        public abstract INotification Transform(INotificationSpaceTransformer transformer);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;

namespace VL.Lib.IO.Notifications
{
    public class GotFocusNotification : NotificationBase
    {
        public GotFocusNotification(object sender, Keys modifierKeys) : base(sender, modifierKeys)
        {
        }

        public override INotification Transform(INotificationSpaceTransformer transformer)
        {
            return this;
        }

        public override INotification WithSender(object sender)
        {
            return new GotFocusNotification(sender, ModifierKeys);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;

namespace VL.Lib.IO.Notifications
{
    /// <summary>
    /// The receiver got the keyboard focus: key notifications go to it now. Windows of the window contract (<see cref="VL.Core.Windowing.IAppWindow"/>)
    /// raise it for the keyboard focus only, on every system; the mouse entering is <see cref="MouseEnterNotification"/>.
    /// Some Windows Forms senders (<c>SkiaControl</c>, the Windows Forms render window of VL.Skia) still raise it when the mouse enters as well.
    /// </summary>
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

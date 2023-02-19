using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Lib.IO.Notifications
{
    public interface INotification
    {
        object Sender { get; }

        bool Handled { get; set; }

        INotification WithSender(object sender);

        INotification Transform(INotificationSpaceTransformer transformer);
    }
}

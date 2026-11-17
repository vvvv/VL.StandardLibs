#nullable enable
using System;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Import;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;

[assembly: ImportType(typeof(GlobalInput), Category = "IO")]

namespace VL.Lib.IO
{
    /// <summary>
    /// The keys and the mouse of the whole desktop, whichever window or program has the focus (<see cref="IPlatformServices.GlobalInput"/>): for a program which shows
    /// what is pressed, such as the display of keys and mouse of the editor. Only where the app can watch the desktop (Windows); elsewhere nothing arrives.
    /// </summary>
    public static class GlobalInput
    {
        /// <summary>
        /// The key and mouse notifications of the whole desktop, as they happen: key down and up with the modifiers, mouse down, up, move, wheel, and a click with its count
        /// after an up. Positions in pixels of the virtual screen. The desktop is only watched while something is subscribed.
        /// </summary>
        public static IObservable<INotification> Notifications => PlatformServices.Default?.GlobalInput ?? Observable.Never<INotification>();
    }
}

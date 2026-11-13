#nullable enable
using System;
using System.Collections.Generic;
using VL.Lib.IO.Notifications;

namespace VL.Core.Windowing
{
    /// <summary>
    /// Creates windows. Registered in the service registry of the app host, the implementation depends on the window system of the host.
    /// </summary>
    public interface IAppWindowService
    {
        /// <summary>
        /// Creates a window on the calling thread, which must be the UI thread of the app, otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        IAppWindow CreateWindow(AppWindowOptions options);

        /// <summary>The windows alive at the moment, in the order of their creation.</summary>
        IReadOnlyList<IAppWindow> Windows { get; }

        /// <summary>Raised on the UI thread after a window was created or disposed. The notification is a <see cref="WindowListChangedNotification"/>.</summary>
        IObservable<INotification> WindowListChanged { get; }
    }
}

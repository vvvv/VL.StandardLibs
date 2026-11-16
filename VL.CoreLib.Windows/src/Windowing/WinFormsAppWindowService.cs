#nullable enable

using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using VL.Core.Windowing;
using VL.Lib.IO.Notifications;

namespace VL.Core.Windows;

/// <summary>
/// Creates <see cref="WinFormsAppWindow"/>s: the <see cref="IAppWindowService"/> of the Windows end points (vvvv.exe, the runner, exported apps).
/// The thread that creates the service is the UI thread: windows are created and used there, a call from another thread throws.
/// </summary>
public sealed class WinFormsAppWindowService : IAppWindowService
{
    private readonly Thread thread = Thread.CurrentThread;
    private readonly List<WinFormsAppWindow> windows = new();
    private readonly Subject<INotification> listChanged = new();

    public IReadOnlyList<IAppWindow> Windows
    {
        get
        {
            Check();
            return windows.ToArray();
        }
    }

    public IObservable<INotification> WindowListChanged => listChanged;

    public IAppWindow CreateWindow(AppWindowOptions options)
    {
        Check();
        var window = new WinFormsAppWindow(this, options);
        windows.Add(window);
        listChanged.OnNext(new WindowListChangedNotification(window, true, this));
        if (options.IsFullscreen)
            ((IAppWindow)window).IsFullscreen = true;
        if (options.Visible)
            window.Show();
        return window;
    }

    internal void OnWindowDisposed(WinFormsAppWindow window)
    {
        if (windows.Remove(window))
            listChanged.OnNext(new WindowListChangedNotification(window, false, this));
    }

    private void Check()
    {
        if (Thread.CurrentThread != thread)
            throw new InvalidOperationException("Windows are created on the UI thread of the app (the thread that created the window service).");
    }
}
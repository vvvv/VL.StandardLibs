#nullable enable
using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using VL.Core.Windowing;

namespace VL.Core
{
    public static class PlatformServices
    {
        public static IPlatformServices Default => AppHost.CurrentOrGlobal.Services.GetService<IPlatformServices>();
    }

    public interface IPlatformServices
    {
        IClipboard Clipboard { get; }
        Optional<string> ShowFileDialog(string initialDirectory, string filter = "All files (*.*)|*.*");
        Optional<string> ShowDirectoryDialog(string initialDirectory, string selectedPath);

        /// <summary>
        /// Asks the person for existing files without blocking the caller: the task gives their paths (one unless <paramref name="allowMultiple"/>), none when the person cancels
        /// or the host has no dialogs (the default). <paramref name="filter"/> names the kinds of files with their patterns: "Text files|*.txt;*.log|All files|*.*".
        /// </summary>
        Task<IReadOnlyList<string>> ShowOpenFileDialogAsync(string? title, string? initialDirectory, string filter = "All files (*.*)|*.*", bool allowMultiple = false) => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        /// <summary>
        /// Asks the person for the path to save a file to without blocking the caller: the task gives the path, none when the person cancels or the host has no dialogs (the default).
        /// <paramref name="fileName"/> is the name the dialog proposes; <paramref name="filter"/> as for <see cref="ShowOpenFileDialogAsync"/>.
        /// </summary>
        Task<Optional<string>> ShowSaveFileDialogAsync(string? title, string? initialDirectory, string? fileName, string filter = "All files (*.*)|*.*") => Task.FromResult(default(Optional<string>));

        /// <summary>The displays (monitors) of the system. Hosts that do not know them return <see cref="NoDisplays.Instance"/>.</summary>
        IDisplays Displays => NoDisplays.Instance;

        /// <summary>
        /// The keys and the mouse of the whole desktop, whichever window or program has the focus: <see cref="Lib.IO.Notifications.KeyDownNotification"/> and
        /// <see cref="Lib.IO.Notifications.KeyUpNotification"/> (an auto repeat is another down) with the modifiers held at that moment; mouse down, up, move, wheel and
        /// horizontal wheel, and after an up a <see cref="Lib.IO.Notifications.MouseClickNotification"/> with the count of the click (2 for a double click).
        /// Positions are in pixels of the virtual screen, as <see cref="DisplayInfo.Bounds"/>; the client area is empty. A subscriber gets them on the
        /// <see cref="System.Threading.SynchronizationContext"/> it subscribed on, if it has one.
        /// Nothing is watched while nobody subscribes: the first subscriber starts the watching, the last one to end its subscription stops it.
        /// Hosts that cannot watch the desktop (the default) give an observable that never fires.
        /// </summary>
        IObservable<Lib.IO.Notifications.INotification> GlobalInput => System.Reactive.Linq.Observable.Never<Lib.IO.Notifications.INotification>();
    }

    public interface IClipboard
    {
        public string GetText();
        public void SetText(string text);

        /// <summary>The picture on the clipboard, null when there is none or the host cannot read pictures (the default).</summary>
        public Lib.Basics.Imaging.IImage? GetImage() => null;

        /// <summary>Puts the picture on the clipboard in place of what was there. Does nothing on a host which cannot (the default).</summary>
        public void SetImage(Lib.Basics.Imaging.IImage image) { }

        /// <summary>
        /// Fires when the content of the clipboard changed, by this program or another; a program which puts several formats on it at once makes one change. Nothing is
        /// watched while nobody subscribes. A subscriber is called on the <see cref="System.Threading.SynchronizationContext"/> it subscribed on, if it has one.
        /// Hosts which cannot watch the clipboard give an observable that never fires (the default).
        /// </summary>
        public IObservable<Unit> Changed => System.Reactive.Linq.Observable.Never<Unit>();
    }

    public static class Clipboard
    {
        public static string GetText() => PlatformServices.Default.Clipboard?.GetText();

        public static void SetText(string text) => PlatformServices.Default.Clipboard?.SetText(text);

        /// <summary>The picture on the clipboard, null when there is none or the app cannot read pictures.</summary>
        public static Lib.Basics.Imaging.IImage? GetImage() => PlatformServices.Default?.Clipboard?.GetImage();

        /// <summary>Puts the picture on the clipboard in place of what was there.</summary>
        public static void SetImage(Lib.Basics.Imaging.IImage image) => PlatformServices.Default?.Clipboard?.SetImage(image);

        /// <summary>Fires when the content of the clipboard changed; the clipboard is only watched while something is subscribed.</summary>
        public static IObservable<Unit> Changed => PlatformServices.Default?.Clipboard?.Changed ?? System.Reactive.Linq.Observable.Never<Unit>();
    }

    /// <summary>The displays of the system. Everything on the UI thread.</summary>
    public interface IDisplays
    {
        IReadOnlyList<DisplayInfo> All { get; }

        DisplayInfo? Primary { get; }

        /// <summary>The display that contains the point (virtual screen pixels), or the nearest one.</summary>
        DisplayInfo? FromPoint(Int2 pixel);

        /// <summary>Raised when a display was connected, disconnected or changed.</summary>
        IObservable<Unit> Changed { get; }
    }

    /// <param name="Bounds">In virtual screen pixels.</param>
    /// <param name="WorkArea">The part of <paramref name="Bounds"/> that is not taken by system bars, in virtual screen pixels.</param>
    /// <param name="ScaleFactor">Pixels per DIP.</param>
    /// <param name="RefreshRate">In Hz, 0 if unknown.</param>
    /// <param name="CanCoverWithFullscreenWindow">False for a display that is known but that the window system gives no handle for right now (macOS: the screen sleeps, the display comes from CoreGraphics): a request for a fullscreen window on it is refused, <see cref="Windowing.IAppWindow.IsFullscreen"/> stays false.</param>
    public sealed record DisplayInfo(int Id, string Name, RectangleF Bounds, RectangleF WorkArea, float ScaleFactor, float RefreshRate, bool IsPrimary, bool CanCoverWithFullscreenWindow = true);

    public sealed class NoDisplays : IDisplays
    {
        public static NoDisplays Instance { get; } = new();

        public IReadOnlyList<DisplayInfo> All => Array.Empty<DisplayInfo>();
        public DisplayInfo? Primary => null;
        public DisplayInfo? FromPoint(Int2 pixel) => null;
        public IObservable<Unit> Changed => System.Reactive.Linq.Observable.Never<Unit>();
    }
}

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
        /// Asks the person for an existing file without blocking the caller: the task gives its path, none when the person cancels or the host has no dialogs (the default).
        /// <paramref name="filter"/> names the kinds of files with their patterns: "Text files|*.txt;*.log|All files|*.*".
        /// </summary>
        Task<Optional<string>> ShowOpenFileDialogAsync(string? title, string? initialDirectory, string filter = "All files (*.*)|*.*") => Task.FromResult(default(Optional<string>));

        /// <summary>
        /// Asks the person for the path to save a file to without blocking the caller: the task gives the path, none when the person cancels or the host has no dialogs (the default).
        /// <paramref name="fileName"/> is the name the dialog proposes; <paramref name="filter"/> as for <see cref="ShowOpenFileDialogAsync"/>.
        /// </summary>
        Task<Optional<string>> ShowSaveFileDialogAsync(string? title, string? initialDirectory, string? fileName, string filter = "All files (*.*)|*.*") => Task.FromResult(default(Optional<string>));

        /// <summary>The displays (monitors) of the system. Hosts that do not know them return <see cref="NoDisplays.Instance"/>.</summary>
        IDisplays Displays => NoDisplays.Instance;
    }

    public interface IClipboard
    {
        public string GetText();
        public void SetText(string text);
    }

    public static class Clipboard
    {
        public static string GetText() => PlatformServices.Default.Clipboard?.GetText();

        public static void SetText(string text) => PlatformServices.Default.Clipboard?.SetText(text);
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

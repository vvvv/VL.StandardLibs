#nullable enable
using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Reactive;
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
    public sealed record DisplayInfo(int Id, string Name, RectangleF Bounds, RectangleF WorkArea, float ScaleFactor, float RefreshRate, bool IsPrimary);

    public sealed class NoDisplays : IDisplays
    {
        public static NoDisplays Instance { get; } = new();

        public IReadOnlyList<DisplayInfo> All => Array.Empty<DisplayInfo>();
        public DisplayInfo? Primary => null;
        public DisplayInfo? FromPoint(Int2 pixel) => null;
        public IObservable<Unit> Changed => System.Reactive.Linq.Observable.Never<Unit>();
    }
}

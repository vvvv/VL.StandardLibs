using System;
using System.Reactive;
using System.Reactive.Linq;

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
    }

    public interface IClipboard
    {
        public string GetText();
        public void SetText(string text);

#nullable enable
        /// <summary>The picture on the clipboard, null when there is none or the host cannot read pictures (the default).</summary>
        public Lib.Basics.Imaging.IImage? GetImage() => null;

        /// <summary>Puts the picture on the clipboard in place of what was there. Does nothing on a host which cannot (the default).</summary>
        public void SetImage(Lib.Basics.Imaging.IImage image) { }

        /// <summary>
        /// A number which changes whenever the content of the clipboard changes, whoever changes it. A program which watches <see cref="Changed"/> recognizes what it
        /// put on the clipboard itself by the count right after its write. 0 on a host which does not count (the default).
        /// </summary>
        public long ChangeCount => 0;

        /// <summary>
        /// Fires when the content of the clipboard changed, by another program or by this one; a program which puts several formats on it at once makes one change.
        /// Nothing is watched while nobody subscribes. A subscriber is called on the <see cref="System.Threading.SynchronizationContext"/> it subscribed on, if it
        /// has one. Hosts which cannot watch the clipboard give an observable that never fires (the default).
        /// </summary>
        public IObservable<Unit> Changed => Observable.Never<Unit>();
#nullable restore
    }

    public static class Clipboard
    {
        public static string GetText() => PlatformServices.Default.Clipboard?.GetText();

        public static void SetText(string text) => PlatformServices.Default.Clipboard?.SetText(text);

#nullable enable
        /// <summary>The picture on the clipboard, null when there is none or the app cannot read pictures.</summary>
        public static Lib.Basics.Imaging.IImage? GetImage() => PlatformServices.Default.Clipboard?.GetImage();

        /// <summary>Puts the picture on the clipboard in place of what was there.</summary>
        /// <param name="image">The picture.</param>
        /// <param name="changeCount">The <see cref="ChangeCount"/> of the clipboard with the picture on it: who watches <see cref="Changed"/> recognizes its own picture by it.</param>
        public static void SetImage(Lib.Basics.Imaging.IImage image, out long changeCount)
        {
            var clipboard = PlatformServices.Default.Clipboard;
            clipboard?.SetImage(image);
            changeCount = clipboard?.ChangeCount ?? 0;
        }

        /// <summary>A number which changes whenever the content of the clipboard changes, whoever changes it.</summary>
        public static long ChangeCount => PlatformServices.Default.Clipboard?.ChangeCount ?? 0;

        /// <summary>Fires when the content of the clipboard changed, by another program or by this one; the clipboard is only watched while something is subscribed.</summary>
        public static IObservable<Unit> Changed => PlatformServices.Default.Clipboard?.Changed ?? Observable.Never<Unit>();
#nullable restore
    }
}

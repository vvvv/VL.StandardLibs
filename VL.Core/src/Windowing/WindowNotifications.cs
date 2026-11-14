#nullable enable
using Stride.Core.Mathematics;
using System.Collections.Generic;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;

namespace VL.Core.Windowing
{
    // Notifications of the window itself. Mouse, key, touch and focus notifications of a window are the ones of VL.Lib.IO.Notifications.
    // All sizes and positions are in pixels. Notifications that carry no position of the client area do not change in a space transform.

    public sealed class WindowResizedNotification : NotificationBase
    {
        /// <summary>Client size in pixels.</summary>
        public readonly Int2 ClientSize;
        public readonly float ScaleFactor;

        public WindowResizedNotification(Int2 clientSize, float scaleFactor, object sender)
            : base(sender, Keys.None)
        {
            ClientSize = clientSize;
            ScaleFactor = scaleFactor;
        }

        public override INotification WithSender(object sender) => new WindowResizedNotification(ClientSize, ScaleFactor, sender);
        public override INotification Transform(INotificationSpaceTransformer transformer) => this;
    }

    public sealed class WindowMovedNotification : NotificationBase
    {
        /// <summary>Position of the client area in virtual screen pixels.</summary>
        public readonly Int2 Position;

        public WindowMovedNotification(Int2 position, object sender)
            : base(sender, Keys.None)
        {
            Position = position;
        }

        public override INotification WithSender(object sender) => new WindowMovedNotification(Position, sender);
        public override INotification Transform(INotificationSpaceTransformer transformer) => this;
    }

    public sealed class WindowStateChangedNotification : NotificationBase
    {
        public readonly WindowState State;
        public readonly bool IsFullscreen;

        public WindowStateChangedNotification(WindowState state, bool isFullscreen, object sender)
            : base(sender, Keys.None)
        {
            State = state;
            IsFullscreen = isFullscreen;
        }

        public override INotification WithSender(object sender) => new WindowStateChangedNotification(State, IsFullscreen, sender);
        public override INotification Transform(INotificationSpaceTransformer transformer) => this;
    }

    public sealed class WindowDpiChangedNotification : NotificationBase
    {
        public readonly float OldScaleFactor;
        public readonly float NewScaleFactor;

        public WindowDpiChangedNotification(float oldScaleFactor, float newScaleFactor, object sender)
            : base(sender, Keys.None)
        {
            OldScaleFactor = oldScaleFactor;
            NewScaleFactor = newScaleFactor;
        }

        public override INotification WithSender(object sender) => new WindowDpiChangedNotification(OldScaleFactor, NewScaleFactor, sender);
        public override INotification Transform(INotificationSpaceTransformer transformer) => this;
    }

    /// <summary>The user or the system asks the window to close. Set <see cref="INotification.Handled"/> to keep it open.</summary>
    public sealed class WindowCloseRequestedNotification : NotificationBase
    {
        public WindowCloseRequestedNotification(object sender)
            : base(sender, Keys.None)
        {
        }

        public override INotification WithSender(object sender) => new WindowCloseRequestedNotification(sender) { Handled = Handled };
        public override INotification Transform(INotificationSpaceTransformer transformer) => this;
    }

    /// <summary>The last notification of a window, raised when it is disposed.</summary>
    public sealed class WindowClosedNotification : NotificationBase
    {
        public WindowClosedNotification(object sender)
            : base(sender, Keys.None)
        {
        }

        public override INotification WithSender(object sender) => new WindowClosedNotification(sender);
        public override INotification Transform(INotificationSpaceTransformer transformer) => this;
    }

    /// <summary>Sent by <see cref="IAppWindowService.WindowListChanged"/>.</summary>
    public sealed class WindowListChangedNotification : NotificationBase
    {
        public readonly IAppWindow Window;
        public readonly bool Added;

        public WindowListChangedNotification(IAppWindow window, bool added, object sender)
            : base(sender, Keys.None)
        {
            Window = window;
            Added = added;
        }

        public override INotification WithSender(object sender) => new WindowListChangedNotification(Window, Added, sender);
        public override INotification Transform(INotificationSpaceTransformer transformer) => this;
    }

    /// <summary>
    /// Text the user typed, committed (after composition). Windows without text input support of the system
    /// raise it for the printable characters the keyboard layout produces.
    /// </summary>
    public sealed class TextInputNotification : NotificationBase
    {
        public readonly string Text;

        public TextInputNotification(string text, Keys modifierKeys, object sender)
            : base(sender, modifierKeys)
        {
            Text = text;
        }

        public override INotification WithSender(object sender) => new TextInputNotification(Text, ModifierKeys, sender);
        public override INotification Transform(INotificationSpaceTransformer transformer) => this;
    }

    /// <summary>The text an input method editor is composing at the moment (preedit), not committed yet.</summary>
    public sealed class TextEditingNotification : NotificationBase
    {
        public readonly string Composition;
        public readonly int CursorStart;
        public readonly int CursorLength;

        public TextEditingNotification(string composition, int cursorStart, int cursorLength, Keys modifierKeys, object sender)
            : base(sender, modifierKeys)
        {
            Composition = composition;
            CursorStart = cursorStart;
            CursorLength = cursorLength;
        }

        public override INotification WithSender(object sender) => new TextEditingNotification(Composition, CursorStart, CursorLength, ModifierKeys, sender);
        public override INotification Transform(INotificationSpaceTransformer transformer) => this;
    }

    public sealed class FilesDroppedNotification : NotificationWithPosition
    {
        public readonly IReadOnlyList<string> Paths;

        public FilesDroppedNotification(IReadOnlyList<string> paths, Vector2 position, Vector2 clientArea, Keys modifierKeys, object sender)
            : base(position, clientArea, modifierKeys, sender)
        {
            Paths = paths;
        }

        public override INotification WithSender(object sender) => new FilesDroppedNotification(Paths, Position, ClientArea, ModifierKeys, sender);

        public override INotification Transform(INotificationSpaceTransformer transformer)
            => new FilesDroppedNotification(Paths, transformer.TransformPosition(Position), transformer.TransformSize(ClientArea), ModifierKeys, Sender);
    }

    public sealed class TextDroppedNotification : NotificationWithPosition
    {
        public readonly string Text;

        public TextDroppedNotification(string text, Vector2 position, Vector2 clientArea, Keys modifierKeys, object sender)
            : base(position, clientArea, modifierKeys, sender)
        {
            Text = text;
        }

        public override INotification WithSender(object sender) => new TextDroppedNotification(Text, Position, ClientArea, ModifierKeys, sender);

        public override INotification Transform(INotificationSpaceTransformer transformer)
            => new TextDroppedNotification(Text, transformer.TransformPosition(Position), transformer.TransformSize(ClientArea), ModifierKeys, Sender);
    }
}

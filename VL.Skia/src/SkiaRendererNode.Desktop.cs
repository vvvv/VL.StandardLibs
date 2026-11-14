#nullable enable
#if !WINDOWS
using System;
using System.ComponentModel;
using System.Drawing;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Commands;
using VL.Core.Import;
using VL.Core.Windowing;
using VL.Lang.PublicAPI;
using VL.Lib.Reactive;
using VL.Skia;
using Keys = VL.Lib.IO.Keys;

namespace Graphics.Skia;

/// <summary>
/// Internal renderer node, the flavor for systems without Windows Forms. Same name and same pins as the node of the Windows flavor,
/// so that the patch node <c>Renderer</c> in VL.Skia.vl is the same on every system. The <c>Form</c> output is the <see cref="SkiaRenderer"/>
/// (a window of the app host, not a Form). "Extend Into Title Bar" and "Interaction Width" have no effect here yet.
/// </summary>
[ProcessNode]
[Smell(SymbolSmell.Internal)]
public sealed class SkiaRendererNode : IDisposable
{
    private readonly SkiaRenderer _renderer;
    private readonly CompositeDisposable _disposables = new();
    private readonly IObservable<FormBoundsNotification> _formBoundsNotifications;
    private readonly ICommandList _ourCommands;
    private bool _disposed;
    private bool _lastShowCursor = true;
    private bool _lastVSync = true;
    private string? _lastTitle;
    private ICommandList? _lastCommands;
    private bool _lastEnableKeyboardShortcuts;

    public SkiaRendererNode(NodeContext nodeContext,
                           Rectangle bounds,
                           [DefaultValue(true)] bool saveBounds,
                           bool boundToDocument,
                           bool dialogIfDocumentChanged,
                           IChannel<bool> showPerfMeter,
                           IChannel<bool> alwaysOnTop,
                           IChannel<bool> extendIntoTitleBar)
    {
        _renderer = new SkiaRenderer(nodeContext);
        var window = _renderer.Window;

        if (!bounds.IsEmpty)
            _renderer.SetBounds(new Stride.Core.Mathematics.RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height));

        _formBoundsNotifications = _renderer.BoundsStream
            .Select(r => new FormBoundsNotification(r, _renderer));

        _disposables.Add(alwaysOnTop.Subscribe(v =>
        {
            if (!window.IsDisposed)
                window.IsAlwaysOnTop = v;
        }));

        var session = IDevSession.Current;
        if (session != null && saveBounds)
        {
            var writeBounds = _renderer.BoundsStream
                .Skip(1)
                .Throttle(TimeSpan.FromSeconds(0.5))
                .ObserveOn(nodeContext.AppHost.SynchronizationContext)
                .Subscribe(r =>
                {
                    if (!window.IsDisposed && !window.IsFullscreen)
                        session.CurrentSolution
                            .SetPinValue(nodeContext.Stack, "Bounds", new Rectangle((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height))
                            .Confirm(VL.Model.SolutionUpdateKind.DontCompile);
                });
            _disposables.Add(writeBounds);
        }

        _disposables.Add(window.Notifications.Subscribe(n =>
        {
            if (n is not WindowCloseRequestedNotification close)
                return;

            // if not exported: cancel and hide. User can then show the window again via SetWindowMode
            var isExported = nodeContext.AppHost.IsExported;
            var hide = !isExported;
            var cancel = !isExported;
            if (boundToDocument && !isExported)
            {
                cancel = SessionNodes.CurrentSolution == SessionNodes.CloseDocumentOfNode(System.Linq.Enumerable.LastOrDefault(nodeContext.Stack), dialogIfDocumentChanged);
                hide = false;
            }
            close.Handled = cancel;
            if (hide)
            {
                _renderer.Hidden = true;
                window.Visible = false;
            }
        }));

        var showPatchOfNodeCmd = Command.Create(() => SessionNodes.ShowPatchOfNode(System.Linq.Enumerable.LastOrDefault(nodeContext.Stack)));
        var toggleFullscreenCmd = Command.Create(() => window.IsFullscreen = !window.IsFullscreen);
        var togglePerfMeterCmd = Command.Create(() => showPerfMeter.Value = !showPerfMeter.Value);
        _ourCommands = CommandList.Create(
            new(SessionNodes.OneUp, showPatchOfNodeCmd),
            new(Keys.Control | Keys.D1, showPatchOfNodeCmd),
            new(Keys.F11, toggleFullscreenCmd),
            new(Keys.Alt | Keys.Enter, toggleFullscreenCmd),
            new(Keys.F2, togglePerfMeterCmd)
            );

        window.Visible = true;
        window.Activate();
    }

    /// <summary>
    /// Bounds in DIP.
    /// </summary>
    public IObservable<FormBoundsNotification> FormBoundsNotifications => _formBoundsNotifications;

    /// <summary>
    /// The renderer: a window of the app host. Not a Form on this system.
    /// </summary>
    public SkiaRenderer Form => _renderer;

    /// <summary>
    /// Current client rectangle in DIP.
    /// </summary>
    public RectangleF ClientBounds
    {
        get
        {
            var r = _renderer.ClientBounds;
            return new RectangleF(r.X, r.Y, r.Width, r.Height);
        }
    }

    /// <summary>No title bar of our own on this system yet.</summary>
    public int InteractionWidth
    {
        set { }
    }

    public void Update(ILayer? input,
                       [DefaultValue("Skia")] string title,
                       [DefaultValue(true)] bool showCursor,
                       [DefaultValue(true)] bool vSync,
                       ICommandList? commands,
                       [DefaultValue(true)] bool enableKeyboardShortcuts,
                       [DefaultValue(true)] bool enabled,
                       out float renderTime)
    {
        var window = _renderer.Window;
        if (_disposed || window.IsDisposed)
        {
            renderTime = 0;
            return;
        }

        if (!string.Equals(_lastTitle, title, StringComparison.Ordinal))
        {
            _lastTitle = title;
            window.Title = title;
        }

        if (_lastShowCursor != showCursor)
        {
            _lastShowCursor = showCursor;
            window.Cursor = showCursor ? WindowCursor.Default : WindowCursor.Hidden;
        }

        if (_lastVSync != vSync)
        {
            _lastVSync = vSync;
            _renderer.VSync = vSync;
        }

        if (!ReferenceEquals(_lastCommands, commands) || _lastEnableKeyboardShortcuts != enableKeyboardShortcuts)
        {
            _lastCommands = commands;
            _lastEnableKeyboardShortcuts = enableKeyboardShortcuts;
            if (enableKeyboardShortcuts)
                _renderer.CommandList = CommandList.CombineRange([commands, _ourCommands, SessionNodes.DevCommands]);
            else
                _renderer.CommandList = commands;
        }

        _renderer.Input = input;
        if (enabled)
            _renderer.Update();

        renderTime = _renderer.RenderTime;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _disposables.Dispose();
        _renderer.Dispose();
    }
}
#endif

#nullable enable

extern alias sw;

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;
using VL.Core;
using VL.Core.Commands;
using VL.Core.Import;
using VL.Lang.PublicAPI;
using VL.Lib.Reactive;
using VL.Skia;
using Keys = VL.Lib.IO.Keys;
using sw::VL.Core.Windows;

namespace Graphics.Skia;

/// <summary>
/// Internal renderer node. Only visible to VL.Skia. On patch side only ILayer related things (Space, Clear, PerfMeter) are built on top.
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
                           bool alwaysOnTop,
                           bool extendIntoTitleBar)
    {
        _renderer = new SkiaRenderer(nodeContext, new (alwaysOnTop, extendIntoTitleBar))
        {
            Text = "Skia",
            FormBorderStyle = FormBorderStyle.Sizable
        };

        if (!bounds.IsEmpty)
            _renderer.SetSize(bounds);

        _formBoundsNotifications = _renderer.BoundsStream
            .Select(r => new FormBoundsNotification(r, _renderer));

        var session = IDevSession.Current;
        if (session != null)
        {
            if (saveBounds)
            {
                var writeBounds = _renderer.BoundsChanged
                    .Throttle(TimeSpan.FromSeconds(0.5))
                    .Subscribe(r =>
                    {
                        session.CurrentSolution
                            .SetPinValue(nodeContext.Stack, "Bounds", r)
                            .Confirm(VL.Model.SolutionUpdateKind.DontCompile);
                    });
                _disposables.Add(writeBounds);
            }
        }

        _renderer.FormClosing += (s, e) =>
        {
            // if not exported: cancel and hide. User can then show the window again via SetWindowMode
            var isExported = nodeContext.AppHost.IsExported;
            var hide = !isExported;
            var cancel = !isExported;
            if (boundToDocument && !isExported)
            {
                cancel = SessionNodes.CurrentSolution == SessionNodes.CloseDocumentOfNode(nodeContext.Stack.LastOrDefault(), dialogIfDocumentChanged);
                hide = false;
            }
            e.Cancel = cancel;
            if (hide)
                _renderer.Hide();
        };

        var showPatchOfNodeCmd = Command.Create(() => SessionNodes.ShowPatchOfNode(nodeContext.Stack.LastOrDefault()));
        var toggleFullscreenCmd = Command.Create(() => _renderer.FullScreen = !_renderer.FullScreen);
        var makeScreenshotCmd = Command.Create(() =>
        {
            var screenShotService = nodeContext.AppHost.Services.GetService<IScreenshotService>();
            screenShotService?.ScreenshotHandle(_renderer.Handle);
        });
        var togglePerfMeterCmd = Command.Create(() => showPerfMeter.Value = !showPerfMeter.Value);
        _ourCommands = CommandList.Create(
            new(SessionNodes.OneUp, showPatchOfNodeCmd),
            new(Keys.Control | Keys.D1, showPatchOfNodeCmd),
            new(Keys.F11, toggleFullscreenCmd),
            new(Keys.Alt | Keys.Enter, toggleFullscreenCmd),
            new(Keys.Control | Keys.D2, makeScreenshotCmd),
            new(Keys.F2, togglePerfMeterCmd)
            );

        _renderer.MouseEnter += (s, e) =>
        {
            if (!_lastShowCursor)
                Cursor.Hide();
        };
        _renderer.MouseLeave += (s, e) =>
        {
            Cursor.Show();
        };

        _renderer.Show();
        _renderer.Activate();
    }

    /// <summary>
    /// Bounds in DIP.
    /// </summary>
    public IObservable<FormBoundsNotification> FormBoundsNotifications => _formBoundsNotifications;

    /// <summary>
    /// Returns the underlying SkiaRenderer Form.
    /// </summary>
    public SkiaRenderer Form => _renderer;

    /// <summary>
    /// Current client rectangle in DIP.
    /// </summary>
    public RectangleF ClientBounds
    {
        get
        {
            var r = _renderer.Bounds;
            return new RectangleF(r.X, r.Y, r.Width, r.Height);
        }
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
        if (_disposed)
        {
            renderTime = 0;
            return;
        }

        // Title
        if (!string.Equals(_lastTitle, title, StringComparison.Ordinal))
        {
            _lastTitle = title;
            _renderer.Text = title;
        }

        // Cursor visibility toggling
        if (_lastShowCursor != showCursor)
        {
            _lastShowCursor = showCursor;
        }

        // VSync
        if (_lastVSync != vSync)
        {
            _lastVSync = vSync;
            _renderer.VSync = vSync;
        }

        // Commands
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
        {
            _renderer.Update();
        }

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
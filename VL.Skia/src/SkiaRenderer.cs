#nullable enable
using Stride.Core.Mathematics;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VL.Core;
using VL.Core.Commands;
using VL.Core.Windowing;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;

namespace VL.Skia
{
    /// <summary>
    /// The render window of patches: a window of the app host (<see cref="IAppWindow"/>, on Windows the Windows Forms window of VL.CoreLib.Windows)
    /// drawn to with Skia (<see cref="ISkiaWindowSurface"/>). Exposed to the patch by the <c>Window</c> output of the renderer node.
    /// </summary>
    public sealed class SkiaRenderer : IDisposable, IProjectionSpace, IWorldSpace2d
    {
        readonly AppHost appHost;
        readonly ISkiaWindowSurface surface;
        readonly IDisposable notificationSubscription;
        readonly BehaviorSubject<RectangleF> boundsStream = new(new RectangleF());
        bool firstRenderCall = true;
        bool hasValidLayer;
        ILayer? layer;
        bool disposed;

        /// <param name="nodeContext">The node that renders.</param>
        /// <param name="options">How the window is made. It is made hidden, with the graphics API Skia needs on this system; the node shows it once it has its bounds.</param>
        public SkiaRenderer(NodeContext nodeContext, AppWindowOptions options)
        {
            appHost = nodeContext.AppHost;
            var windows = appHost.Services.GetService<IAppWindowService>()
                ?? throw new InvalidOperationException("This app has no window system: a Renderer needs an app host that offers windows.");

            Window = windows.CreateWindow(options with
            {
                Graphics = SkiaWindowSurface.GraphicsApi,
                Visible = false,
                Owner = this
            });
            surface = SkiaWindowSurface.Create(Window, appHost);

            notificationSubscription = Window.Notifications.Subscribe(OnNotification);
            PublishBounds();
        }

        public IAppWindow Window { get; }

        /// <summary>The outer bounds of the window (with its frame) in DIP, a new value whenever it moves or resizes.</summary>
        public IObservable<RectangleF> BoundsStream => boundsStream;

        public float RenderTime => surface.RenderTime;

        public bool VSync
        {
            get => surface.VSync;
            set => surface.VSync = value;
        }

        public ICommandList? CommandList { get; set; }

        /// <summary>Obsolete, the outer bounds in DIP. Use <see cref="BoundsStream"/>.</summary>
        public BehaviorSubject<System.Drawing.Rectangle> BoundsChanged { get; } = new(new System.Drawing.Rectangle());

        public Mouse Mouse => (Mouse)Window.Mouse;

        public Keyboard Keyboard => (Keyboard)Window.Keyboard;

        public TouchDevice TouchDevice => (TouchDevice)Window.TouchDevice;

        public bool FullScreen
        {
            get => Window.IsFullscreen;
            set => Window.IsFullscreen = value;
        }

        /// <summary>Called from the patch. The outer bounds in DIP.</summary>
        public void SetSize(System.Drawing.Rectangle boundsInDIP)
            => SetBounds(new RectangleF(boundsInDIP.X, boundsInDIP.Y, boundsInDIP.Width, boundsInDIP.Height));

        public ILayer? Input
        {
            get => layer;
            set
            {
                hasValidLayer = true;
                layer = value;
            }
        }

        public bool IsDisposed => disposed;

        /// <summary>
        /// The window was hidden because the user closed it (in the editor closing only hides): nothing is drawn and the layer gets no notifications
        /// until the window is visible again. Not the visibility of the window itself: a window of a headless host is never shown and is drawn anyway.
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>The client area in DIP: its position on the screen and its size.</summary>
        public RectangleF ClientBounds
        {
            get
            {
                var scale = Window.ScaleFactor;
                var p = Window.Position;
                var s = Window.ClientSize;
                return new RectangleF(p.X / scale, p.Y / scale, s.X / scale, s.Y / scale);
            }
        }

        /// <summary>The outer bounds of the window, the client area with the frame, in DIP.</summary>
        public RectangleF OuterBounds
        {
            get
            {
                var scale = Window.ScaleFactor;
                var m = Window.FrameMargins;
                var p = Window.Position;
                var s = Window.ClientSize;
                return new RectangleF((p.X - m.Left) / scale, (p.Y - m.Top) / scale, (s.X + m.Left + m.Right) / scale, (s.Y + m.Top + m.Bottom) / scale);
            }
        }

        /// <summary>Sets the outer bounds of the window (with its frame), in DIP. Bounds that are not on any display are centered on the primary one.</summary>
        public void SetBounds(RectangleF bounds)
        {
            if (bounds.IsEmpty)
                return;

            var scale = Window.ScaleFactor;
            var m = Window.FrameMargins;
            var outerPosition = new Int2((int)MathF.Round(bounds.X * scale), (int)MathF.Round(bounds.Y * scale));
            var outerSize = new Int2((int)MathF.Round(bounds.Width * scale), (int)MathF.Round(bounds.Height * scale));

            var displays = PlatformServices.Default?.Displays;
            if (displays != null && displays.All.Count > 0)
            {
                var visible = false;
                foreach (var d in displays.All)
                    visible |= outerPosition.X < d.Bounds.Right && outerPosition.X + outerSize.X > d.Bounds.Left && outerPosition.Y < d.Bounds.Bottom && outerPosition.Y + outerSize.Y > d.Bounds.Top;
                if (!visible && displays.Primary is { } primary)
                {
                    var area = primary.WorkArea;
                    outerPosition = new Int2((int)(area.X + (area.Width - outerSize.X) / 2), (int)(area.Y + (area.Height - outerSize.Y) / 2));
                }
            }

            var position = new Int2(outerPosition.X + m.Left, outerPosition.Y + m.Top);
            var size = new Int2(Math.Max(1, outerSize.X - m.Left - m.Right), Math.Max(1, outerSize.Y - m.Top - m.Bottom));
            if (Window.Position != position || Window.ClientSize != size)
                Window.SetBounds(position, size);
        }

        /// <summary>Draws the layer, and the caption buttons where the title bar is extended into the client area. Called once per frame by the node.</summary>
        public void Update()
        {
            if (disposed || Window.IsDisposed)
                return;
            if (Hidden && Window.Visible)
                Hidden = false;
            if (Hidden)
                return;

            var titleBar = Window.GetFeature<IWindowTitleBar>();
            surface.Render(callerInfo =>
            {
                using var _ = appHost.MakeCurrentIfNone();
                try
                {
                    if (!firstRenderCall && hasValidLayer)
                        layer?.Render(callerInfo);
                    if (titleBar != null)
                        TitleBarButtons.Paint(callerInfo.Canvas, titleBar, Window.State == WindowState.Maximized, Window.ScaleFactor);
                    firstRenderCall = false;
                }
                catch (Exception exception)
                {
                    RuntimeGraph.ReportException(exception);
                }
            });
        }

        void PublishBounds()
        {
            var b = OuterBounds;
            BoundsChanged.OnNext(new System.Drawing.Rectangle((int)b.X, (int)b.Y, (int)b.Width, (int)b.Height));
            boundsStream.OnNext(b);
        }

        void OnNotification(INotification n)
        {
            if (disposed)
                return;

            if (n is WindowResizedNotification or WindowMovedNotification or WindowDpiChangedNotification)
            {
                PublishBounds();
                // The layer learns about the new size of the client area (in DIP)
                n = new NotificationWithClientArea(ClientBounds.Size.ToVector2(), Keys.None, this);
            }
            else if (n is not (MouseNotification or KeyNotification or TouchNotification or GotFocusNotification or LostFocusNotification))
                return;

            using var _ = appHost.MakeCurrentIfNone();
            try
            {
                if (CommandList != null && CommandList.TryExecute(n))
                    n.Handled = true;
                else if (!Hidden && hasValidLayer && layer != null)
                    n.Handled = layer.Notify(n, surface.CallerInfo);
            }
            catch (Exception exception)
            {
                RuntimeGraph.ReportException(exception);
            }
        }

        public void MapFromPixels(INotificationWithPosition notification, out Vector2 inNormalizedProjection, out Vector2 inProjection)
            => SpaceHelpers.DoMapFromPixels(notification.Position, notification.ClientArea, out inNormalizedProjection, out inProjection);

        public Vector2 MapFromPixels(INotificationWithPosition notification) => notification.Position;

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            notificationSubscription.Dispose();
            boundsStream.Dispose();
            surface.Dispose();
            if (!Window.IsDisposed)
                Window.Dispose();
        }
    }
}

#nullable enable
#if !WINDOWS
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
    /// The render window of patches on systems without Windows Forms (Linux, macOS): a window of the app host (<see cref="IAppWindow"/>)
    /// drawn to with Skia (<see cref="ISkiaWindowSurface"/>). The Windows flavor of VL.Skia has a type of the same name that is a Form.
    /// Exposed to the patch by the <c>Form</c> output of the renderer node.
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

        public SkiaRenderer(NodeContext nodeContext)
        {
            appHost = nodeContext.AppHost;
            var windows = appHost.Services.GetService<IAppWindowService>()
                ?? throw new InvalidOperationException("This app has no window system: a Renderer needs an app host that offers windows (the desktop host).");

            Window = windows.CreateWindow(AppWindowOptions.Default with
            {
                Title = "Skia",
                Graphics = WindowGraphicsApi.OpenGL,
                // Shown by the node after it has set the bounds
                Visible = false,
                Owner = this
            });
            surface = GlWindowSurface.TryCreate(Window)
                ?? throw new InvalidOperationException("The window has no OpenGL context to draw to.");

            notificationSubscription = Window.Notifications.Subscribe(OnNotification);
            PublishBounds();
        }

        public IAppWindow Window { get; }

        /// <summary>The client bounds of the window in DIP, a new value whenever it moves or resizes.</summary>
        public IObservable<RectangleF> BoundsStream => boundsStream;

        public float RenderTime => surface.RenderTime;

        public bool VSync
        {
            get => surface.VSync;
            set => surface.VSync = value;
        }

        public ICommandList? CommandList { get; set; }

        // The members the Windows flavor has and the document VL.Skia.vl forwards
        public Mouse Mouse => (Mouse)Window.Mouse;

        public Keyboard Keyboard => (Keyboard)Window.Keyboard;

        public TouchDevice TouchDevice => (TouchDevice)Window.TouchDevice;

        public bool FullScreen
        {
            get => Window.IsFullscreen;
            set => Window.IsFullscreen = value;
        }

        /// <summary>Called from the patch. The bounds are in DIP.</summary>
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

        /// <summary>The patch hid the window (closing it in the editor): nothing is drawn and the layer gets no notifications.</summary>
        public bool Hidden { get; set; }

        /// <summary>Client bounds in DIP.</summary>
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

        /// <summary>Sets position and client size of the window. The bounds are in DIP. Bounds that are not on any display are centered on the primary one.</summary>
        public void SetBounds(RectangleF bounds)
        {
            if (bounds.IsEmpty)
                return;

            var scale = Window.ScaleFactor;
            var position = new Int2((int)MathF.Round(bounds.X * scale), (int)MathF.Round(bounds.Y * scale));
            var size = new Int2(Math.Max(1, (int)MathF.Round(bounds.Width * scale)), Math.Max(1, (int)MathF.Round(bounds.Height * scale)));

            var displays = PlatformServices.Default?.Displays;
            if (displays != null && displays.All.Count > 0)
            {
                var visible = false;
                foreach (var d in displays.All)
                    visible |= position.X < d.Bounds.Right && position.X + size.X > d.Bounds.Left && position.Y < d.Bounds.Bottom && position.Y + size.Y > d.Bounds.Top;
                if (!visible && displays.Primary is { } primary)
                {
                    var area = primary.WorkArea;
                    position = new Int2((int)(area.X + (area.Width - size.X) / 2), (int)(area.Y + (area.Height - size.Y) / 2));
                }
            }

            if (Window.Position != position || Window.ClientSize != size)
                Window.SetBounds(position, size);
        }

        /// <summary>Draws the layer. Called once per frame by the node.</summary>
        public void Update()
        {
            if (disposed || Window.IsDisposed || Hidden)
                return;

            surface.Render(callerInfo =>
            {
                using var _ = appHost.MakeCurrentIfNone();
                try
                {
                    if (!firstRenderCall && hasValidLayer)
                        layer?.Render(callerInfo);
                    firstRenderCall = false;
                }
                catch (Exception exception)
                {
                    RuntimeGraph.ReportException(exception);
                }
            });
        }

        void PublishBounds() => boundsStream.OnNext(ClientBounds);

        void OnNotification(INotification n)
        {
            if (disposed)
                return;

            if (n is WindowResizedNotification or WindowMovedNotification or WindowDpiChangedNotification)
            {
                PublishBounds();
                // Same as on Windows: the layer learns about the new size of the client area (in DIP)
                n = new NotificationWithClientArea(boundsStream.Value.Size.ToVector2(), Keys.None, this);
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
#endif

namespace Stride.Input
{
    using Stride.Core.Collections;
    using Stride.Core.Mathematics;

    public class MappedMouse : IMouseDevice, IMappedDevice
    {
        private readonly IMouseDevice mouse;
        private readonly MappedInputSource source;

        public MappedMouse(IMouseDevice mouse, MappedInputSource source)
        {
            this.mouse = mouse;
            this.source = source;
        }

        public Guid SourceDeviceId => mouse.Id;

        public Vector2 Position => mouse.Position.transformPos(mouse, source);
        
        public Vector2 Delta => mouse.Delta.transformDelta(mouse, source);

        public IReadOnlySet<MouseButton> PressedButtons => mouse.PressedButtons;

        public IReadOnlySet<MouseButton> ReleasedButtons => mouse.ReleasedButtons;

        public IReadOnlySet<MouseButton> DownButtons => mouse.DownButtons;

        public bool IsPositionLocked => mouse.IsPositionLocked;

        public Vector2 SurfaceSize => source.Viewport.Size;

        public float SurfaceAspectRatio => source.Viewport.Size.Y / source.Viewport.Size.X;

        public IReadOnlySet<PointerPoint> PressedPointers => mouse.PressedPointers.transform(mouse, source, this);

        public IReadOnlySet<PointerPoint> ReleasedPointers => mouse.ReleasedPointers.transform(mouse, source, this);

        public IReadOnlySet<PointerPoint> DownPointers => mouse.DownPointers.transform(mouse, source, this);

        public string Name => "Mapped Mouse";

        public Guid Id => Guid.NewGuid();

        public int Priority => mouse.Priority + 1;

        public IInputSource Source => source;

        int IInputDevice.Priority { get; set; }

        public event EventHandler<SurfaceSizeChangedEventArgs> SurfaceSizeChanged
        {
            add
            {
                mouse.SurfaceSizeChanged += value;
            }

            remove
            {
                mouse.SurfaceSizeChanged -= value;
            }
        }

        public void LockPosition(bool forceCenter = false)
        {
            mouse.LockPosition(forceCenter);
        }

        public void SetPosition(Vector2 normalizedPosition)
        {
            mouse.SetPosition(normalizedPosition);
        }

        public void UnlockPosition()
        {
            mouse.UnlockPosition();
        }

        public void Update(List<InputEvent> inputEvents)
        {
        }
    }
}

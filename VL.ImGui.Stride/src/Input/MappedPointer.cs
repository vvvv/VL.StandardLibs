namespace Stride.Input
{
    using Stride.Core.Collections;
    using Stride.Core.Mathematics;

    public class MappedPointer : PointerBase, IPointerDevice, IMappedDevice
    {
        private readonly IPointerDevice pointer;
        private readonly MappedInputSource source;

        public MappedPointer(IPointerDevice pointer, MappedInputSource source) : base()
        {
            this.pointer = pointer;
            this.source = source;
        }

        public Guid SourceDeviceId => pointer.Id;

        public Vector2 SurfaceSize => source.Viewport.Size;

        public float SurfaceAspectRatio => source.Viewport.Size.Y / source.Viewport.Size.X;

        public IReadOnlySet<PointerPoint> PressedPointers => base.GetPressedPointers(pointer, source, this);
        public IReadOnlySet<PointerPoint> ReleasedPointers => base.GetReleasedPointers(pointer, source, this);
        public IReadOnlySet<PointerPoint> DownPointers => base.GetDownPointers(pointer, source, this);

        public string Name => "Mapped Pointer";

        public Guid Id => Guid.NewGuid();

        public int Priority { get; set; }

        public IInputSource Source => source;

        public event EventHandler<SurfaceSizeChangedEventArgs> SurfaceSizeChanged
        {
            add
            {
                pointer.SurfaceSizeChanged += value;
            }

            remove
            {
                pointer.SurfaceSizeChanged -= value;
            }
        }

        public void Update(List<InputEvent> inputEvents)
        {
        }
    }
}

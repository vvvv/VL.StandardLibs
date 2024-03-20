namespace Stride.Input
{
    using Stride.Core.Collections;
    using Stride.Core.Mathematics;

    public class MappedPointer : IPointerDevice, IMappedDevice
    {
        private readonly IPointerDevice pointer;
        private readonly MappedInputSource source;

        public MappedPointer(IPointerDevice pointer, MappedInputSource source)
        {
            this.pointer = pointer;
            this.source = source;
        }

        public Guid SourceDeviceId => pointer.Id;

        public Vector2 SurfaceSize => source.Viewport.Size;

        public float SurfaceAspectRatio => source.Viewport.Size.Y / source.Viewport.Size.X;

        public IReadOnlySet<PointerPoint> PressedPointers => pointer.PressedPointers.transform(pointer, source, this);

        public IReadOnlySet<PointerPoint> ReleasedPointers => pointer.ReleasedPointers.transform(pointer, source, this);

        public IReadOnlySet<PointerPoint> DownPointers => pointer.DownPointers.transform(pointer, source, this);

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

        //public event EventHandler<SurfaceSizeChangedEventArgs>? SurfaceSizeChanged;

        public void Update(List<InputEvent> inputEvents)
        {
        }
    }
}

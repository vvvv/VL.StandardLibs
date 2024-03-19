using Stride.Core.Mathematics;

namespace Stride.Input
{
    using Stride.Core.Collections;
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

        public Vector2 SurfaceSize => pointer.SurfaceSize;

        public float SurfaceAspectRatio => pointer.SurfaceAspectRatio;

        public IReadOnlySet<PointerPoint> PressedPointers => pointer.PressedPointers;

        public IReadOnlySet<PointerPoint> ReleasedPointers => pointer.ReleasedPointers;

        public IReadOnlySet<PointerPoint> DownPointers => pointer.DownPointers;

        public string Name => "Mapped Pointer";

        public Guid Id => Guid.NewGuid();

        public int Priority { get; set; }

        public IInputSource Source => source;

        public event EventHandler<SurfaceSizeChangedEventArgs>? SurfaceSizeChanged;

        public void Update(List<InputEvent> inputEvents)
        {
        }
    }
}

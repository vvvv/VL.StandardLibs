namespace Stride.Input
{
    using Stride.Core.Collections;
    using Stride.Core.Mathematics;

    public abstract class MappedPointerBase : IPointerDevice, IMappedDevice
    {
        internal IPointerDevice pointer;
        internal readonly MappedInputSource source;

        private readonly HashSet<PointerPoint> pressedPointers;
        private readonly HashSet<PointerPoint> releasedPointers;
        private readonly HashSet<PointerPoint> downPointers;

        private readonly IReadOnlySet<PointerPoint> _pressedPointers;
        private readonly IReadOnlySet<PointerPoint> _releasedPointers;
        private readonly IReadOnlySet<PointerPoint> _downPointers;

        public MappedPointerBase(IPointerDevice pointer, MappedInputSource source)
        {
            this.pointer = pointer; 
            this.source = source;

            pressedPointers = new HashSet<PointerPoint>();
            releasedPointers = new HashSet<PointerPoint>();
            downPointers = new HashSet<PointerPoint>();

            _pressedPointers = new ReadOnlySet<PointerPoint>(pressedPointers);
            _releasedPointers = new ReadOnlySet<PointerPoint>(releasedPointers);
            _downPointers = new ReadOnlySet<PointerPoint>(downPointers);
        }

        public bool SetSourceDevice(IInputDevice device)
        {
            if (device is IPointerDevice pointer)
            {
                this.pointer = pointer;
                return true;
            }
            else
            {
                return false;
            }
        }

        private void transformAndFillSet(IReadOnlySet<PointerPoint> pointers, HashSet<PointerPoint> pointerset)
        {
            pointerset.Clear();

            // make the same length
            // so only add and remove if length not same
            // GC friendly
            if (pointers.Count != pointerset.Count)
            {
                while (pointers.Count < pointerset.Count)
                {
                    var last = pointerset.Last();
                    if (last != null)
                    {
                        pointerset.Remove(last);
                    }
                }
                while (pointers.Count > pointerset.Count)
                {
                    pointerset.Add(new PointerPoint());
                }
            }

            // transform
            using (var orginalE = pointers.GetEnumerator())
            using (var mappedE = pointerset.GetEnumerator())
            {
                while (orginalE.MoveNext() && mappedE.MoveNext())
                {
                    mappedE.Current.Position = orginalE.Current.Position.transformPos(pointer, source);
                    mappedE.Current.Delta = orginalE.Current.Delta.transformDelta(pointer, source);
                    mappedE.Current.IsDown = orginalE.Current.IsDown;
                    mappedE.Current.Id = orginalE.Current.Id;
                    mappedE.Current.Pointer = this;
                }
            }        
        }

        public IReadOnlySet<PointerPoint> GetPressedPointers()
        {
            transformAndFillSet(pointer.PressedPointers, pressedPointers);
            return _pressedPointers;
        }
        public IReadOnlySet<PointerPoint> GetReleasedPointers()
        {
            transformAndFillSet(pointer.ReleasedPointers, releasedPointers);
            return _releasedPointers;
        }
        public IReadOnlySet<PointerPoint> GetDownPointers()
        {
            transformAndFillSet(pointer.DownPointers, downPointers);
            return _downPointers;
        }
        public Guid SourceDeviceId => pointer.Id;

        public Vector2 SurfaceSize => source.Viewport.Size;

        public float SurfaceAspectRatio => source.Viewport.Size.Y / source.Viewport.Size.X;

        public IReadOnlySet<PointerPoint> PressedPointers => GetPressedPointers();
        public IReadOnlySet<PointerPoint> ReleasedPointers => GetReleasedPointers();
        public IReadOnlySet<PointerPoint> DownPointers => GetDownPointers();

        public abstract string Name { get; }

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

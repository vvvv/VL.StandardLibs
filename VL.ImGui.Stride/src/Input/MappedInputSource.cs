using Stride.Core.Mathematics;

namespace Stride.Input
{
    using Stride.Core.Collections;
    using NotifyCollectionChangedAction = System.Collections.Specialized.NotifyCollectionChangedAction;

    public class MappedInputSource : IInputSource
    {
        private readonly InputManager inputManager;
        private readonly IInputSource inputSource;

        public MappedInputSource(InputManager inputManager, IInputSource inputSource)
        {
            this.inputManager = inputManager;
            this.inputSource = inputSource;
            this.inputSource.Devices.CollectionChanged += Devices_CollectionChanged;
            if (!this.inputManager.Sources.Contains(this))
                this.inputManager.Sources.Add(this);
        }

        private void Devices_CollectionChanged(object? sender, TrackingCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddDevice((IInputDevice)e.Item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    var d = Devices.Values.Where(d => d is IHasSourceDevice && ((IHasSourceDevice)d).SourceDeviceId == (Guid)e.Key).FirstOrDefault();
                    if (d != null)
                        Devices.Remove(d.Id);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        public TrackingDictionary<Guid, IInputDevice> Devices { get; } = new TrackingDictionary<Guid, IInputDevice>();

        public void Dispose()
        {
            if (inputManager.Sources.Contains(this)) 
            {
                inputManager.Sources.Remove(this);
            }
            this.inputSource.Devices.CollectionChanged -= Devices_CollectionChanged;
        }

        private void AddDevice(IInputDevice input)
        {
            if (input is IMouseDevice mouse)
            {
                var device = new MappedMouse(mouse, this);
                Devices.Add(device.Id, device);
            }
            else if (input is IPointerDevice pointer)
            {
                var device = new MappedPointer(pointer, this);
                Devices.Add(device.Id, device);
            }
            else if (input is IKeyboardDevice keyboard)
            {
                var device = new MappedKeyboard(keyboard, this);
                Devices.Add(device.Id, device);
            }
        }

        public void Initialize(InputManager inputManager)
        {
            foreach(var kv in inputSource.Devices) 
            {
                AddDevice(kv.Value);
            }
        }

        public void Pause()
        {
            
        }

        public void Resume()
        {
            
        }

        public void Scan()
        {
            
        }

        public void Update()
        {
            
        }
    }

    public interface IHasSourceDevice
    {
        public Guid SourceDeviceId { get; }
    }

    public class MappedMouse : IMouseDevice , IHasSourceDevice
    {
        private readonly IMouseDevice mouse;
        private readonly MappedInputSource source;

        public MappedMouse(IMouseDevice mouse, MappedInputSource source)
        {
            this.mouse = mouse;
            this.source = source;
        }

        public Guid SourceDeviceId => mouse.Id;

        public Vector2 Position => mouse.Position;

        public Vector2 Delta => mouse.Delta;

        public IReadOnlySet<MouseButton> PressedButtons => mouse.PressedButtons;

        public IReadOnlySet<MouseButton> ReleasedButtons => mouse.ReleasedButtons;

        public IReadOnlySet<MouseButton> DownButtons => mouse.DownButtons;

        public bool IsPositionLocked => mouse.IsPositionLocked;

        public Vector2 SurfaceSize => mouse.SurfaceSize;

        public float SurfaceAspectRatio => mouse.SurfaceAspectRatio;

        public IReadOnlySet<PointerPoint> PressedPointers => mouse.PressedPointers;

        public IReadOnlySet<PointerPoint> ReleasedPointers => mouse.ReleasedPointers;

        public IReadOnlySet<PointerPoint> DownPointers => mouse.DownPointers;

        public string Name => "Mapped Mouse";

        public Guid Id => Guid.NewGuid();

        public int Priority => mouse.Priority + 1;

        public IInputSource Source => source;

        int IInputDevice.Priority { get ; set ; }

        public event EventHandler<SurfaceSizeChangedEventArgs>? SurfaceSizeChanged;

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

    public class MappedPointer : IPointerDevice, IHasSourceDevice
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

        public int Priority { get ; set; }

        public IInputSource Source => source;

        public event EventHandler<SurfaceSizeChangedEventArgs>? SurfaceSizeChanged;

        public void Update(List<InputEvent> inputEvents)
        {
        }
    }

    public class MappedKeyboard : IKeyboardDevice, IHasSourceDevice
    {
        private readonly IKeyboardDevice keyboard;
        private readonly MappedInputSource source;

        public MappedKeyboard(IKeyboardDevice keyboard, MappedInputSource source)
        {
            this.keyboard = keyboard;
            this.source = source;
        }

        public Guid SourceDeviceId => keyboard.Id;

        public IReadOnlySet<Keys> PressedKeys => keyboard.PressedKeys;

        public IReadOnlySet<Keys> ReleasedKeys => keyboard.ReleasedKeys;

        public IReadOnlySet<Keys> DownKeys => keyboard.DownKeys;

        public string Name => "Mapped Keyboard";

        public Guid Id => Guid.NewGuid();

        public int Priority { get; set; }

        public IInputSource Source => source;

        public void Update(List<InputEvent> inputEvents)
        {
        }
    }
}

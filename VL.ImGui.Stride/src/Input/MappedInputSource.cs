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

        #region MappedDeviceHandeling
        private void Devices_CollectionChanged(object? sender, TrackingCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddMappedDevice((IInputDevice)e.Item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveMappedDeviceBySourceDeviceID((Guid)e.Key);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }
        private void AddMappedDevice(IInputDevice input)
        {
            if (input is IMouseDevice mouse)
            {
                AddOrUpdateDevice(new MappedMouse(mouse, this));
            }
            else if (input is IPointerDevice pointer)
            {
                AddOrUpdateDevice(new MappedPointer(pointer, this));
            }
            else if (input is IKeyboardDevice keyboard)
            {
                AddOrUpdateDevice(new MappedKeyboard(keyboard, this));
            }
        }
        private void RemoveMappedDeviceBySourceDeviceID(Guid sourceDeviceID)
        {
            var device = GetDeviceBySourceDeviceID(sourceDeviceID);
            if (device != null)
            {
                Devices.Remove(device.Id);
            }
        }

        private void AddOrUpdateDevice(IInputDevice device )
        {
            var sourceDevice = GetDeviceBySourceDeviceID(device.Id);
            if (sourceDevice == null)
            {
                Devices.Add(device.Id, device);
            }
            else
            {
                Devices.Remove(sourceDevice.Id);
                Devices.Add(device.Id, device);
            }
        }

        private IInputDevice? GetDeviceBySourceDeviceID(Guid sourceDeviceID)
        {
            return Devices.Values.Where(d => d is IMappedDevice && ((IMappedDevice)d).SourceDeviceId == sourceDeviceID).FirstOrDefault();
        }
        #endregion MappedDeviceHandeling

        #region IInputSource
        public TrackingDictionary<Guid, IInputDevice> Devices { get; } = new TrackingDictionary<Guid, IInputDevice>();

        public void Initialize(InputManager inputManager)
        {
            foreach(var kv in inputSource.Devices) 
            {
                AddMappedDevice(kv.Value);
            }
        }
        public void Pause() {}
        public void Resume() {}
        public void Scan() {}
        public void Update() {}
        public void Dispose()
        {
            if (inputManager.Sources.Contains(this))
            {
                inputManager.Sources.Remove(this);
            }
            this.inputSource.Devices.CollectionChanged -= Devices_CollectionChanged;
        }

        #endregion IInputSource
    }
}

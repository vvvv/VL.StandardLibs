namespace Stride.Input
{
    using Stride.Graphics;
    using Stride.Core.Collections;
    using NotifyCollectionChangedAction = System.Collections.Specialized.NotifyCollectionChangedAction;
    using VL.Lib.Basics.Resources;
    using VL.Core;
    using VL.Stride;
    using VL.ImGui.Stride;

    public class MappedInputSource : IInputSource
    {
        private readonly IWithViewport withViewport;

        public Viewport Viewport => withViewport.Viewport;

        public MappedInputSource(IWithViewport withViewport)
        {
            this.withViewport = withViewport;
        }

        public void Connect(IInputSource? inputSource) 
        {
            Devices.Clear();
            if (inputSource != null)
            {
                foreach (var kv in inputSource.Devices)
                {
                    AddMappedDevice(kv.Value);
                }
                inputSource.Devices.CollectionChanged += InputDevicesChangend;
            }
        }

        public void Disconnect(IInputSource? inputSource) 
        {
            if (inputSource != null)
            {
                inputSource.Devices.CollectionChanged -= InputDevicesChangend;
            }
            Devices.Clear();
        }

        private void InputDevicesChangend(object? sender, TrackingCollectionChangedEventArgs e)
        {
            switch (e.Action)
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

        #region MappedDeviceHandeling
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

        }
        public void Pause() {}
        public void Resume() {}
        public void Scan() {}
        public void Update() {}
        #endregion IInputSource

        public void Dispose()
        {

        }
    }
}

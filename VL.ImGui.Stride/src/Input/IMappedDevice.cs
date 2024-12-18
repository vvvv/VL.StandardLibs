namespace Stride.Input
{
    public interface IMappedDevice
    {
        public Guid SourceDeviceId { get; }

        public bool SetSourceDevice(IInputDevice device);
    }
}

namespace Stride.Input
{
    public class MappedPointer : MappedPointerBase, IPointerDevice, IMappedDevice
    {
        public MappedPointer(IPointerDevice pointer, MappedInputSource source) : base(pointer, source) { }

        public override string Name => "Mapped Pointer";
    }
}

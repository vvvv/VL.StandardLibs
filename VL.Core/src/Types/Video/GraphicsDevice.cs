namespace VL.Lib.Basics.Video;

public interface IGraphicsDeviceProvider
{
    public GraphicsDeviceType Type { get; }
    public nint NativePointer { get; }
    public bool UseLinearColorspace { get; }
}

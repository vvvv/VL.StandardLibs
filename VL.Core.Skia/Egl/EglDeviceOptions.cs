namespace VL.Core.Skia.Egl;

public sealed record EglDeviceOptions
{
    public static readonly EglDeviceOptions Default = new();

    public bool UseWarpDevice { get; set; }
}

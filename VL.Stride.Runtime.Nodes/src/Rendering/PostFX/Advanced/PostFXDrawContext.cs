using Stride.Graphics;

namespace VL.Stride.Rendering.PostFX.Advanced;

public struct PostFXDrawContext()
{
    public required Texture RenderTarget { get; init; }

    public required Texture ColorBuffer { get; init; }

    public required Texture DepthBuffer { get; init; }

    public Texture? NormalBuffer { get; init; }

    public Texture? SpecularRoughnessBuffer { get; init; }

    public Texture? VelocityBuffer { get; init; }
}

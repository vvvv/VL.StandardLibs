using Stride.Core.Mathematics;
using Stride.Engine;

namespace VL.Stride.Rendering;

public class ViewportRenderInfo
{
    public virtual CameraComponent CameraComponent { get; set; } = new CameraComponent();

    public virtual Vector2 RenderTargetSize { get; set; }
}

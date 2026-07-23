using Stride.Core;
using Stride.Rendering;
using VL.Lib.Mathematics;

namespace VL.Stride.Rendering;

[DataContract]
public class ViewportView
{
    [DataMember]
    public RenderView View;

    [DataMember]
    public ViewportF Viewport;

    [DataMemberIgnore]
    public IGraphicsRendererBase Renderer { get; set; }
}

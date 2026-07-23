using Stride.Core;
using Stride.Engine;

namespace VL.Stride.Rendering;

[DataContract]
public class ViewportSettings
{
    [DataMember]
    public IReadOnlyList<ViewportView> Views { get; set; } = new List<ViewportView>();

    [DataMember]
    public bool Enabled { get; set; }

    [DataMember]
    public ViewportRenderInfo ViewportRenderInfo { get; set; }

    [DataMember]
    public bool UseStereoscopic { get; set; }

    internal bool ShouldUseStereoscopic => Enabled && UseStereoscopic;
}

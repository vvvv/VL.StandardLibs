using VL.Core.Import;
using VL.Core.PublicAPI;

namespace VL.TestNodes;

[ProcessNode]
[Region(SupportedBorderControlPoints = ControlPointType.Border)]
public class IfElseRegion : IRegion<IfElseRegion.IInlay>
{
    private readonly Dictionary<InputDescription, object?> _inputs = new();
    private readonly Dictionary<OutputDescription, object?> _outputs = new();

    IInlay? inlay;

    public IfElseRegion()
    {
    }

    public void Update(bool condition)
    {
        if (inlay is null)
            return;
        if (condition)
            inlay.Then();
        else
            inlay.Else();
    }

    void IRegion<IInlay>.SetPatchInlayFactory(Func<IInlay> patchInlayFactory)
    {
        if (inlay is null)
            inlay = patchInlayFactory();
    }

    void IRegion<IInlay>.AcknowledgeInput(in InputDescription cp, object? outerValue)
    {
        _inputs[cp] = outerValue;
    }

    void IRegion<IInlay>.AcknowledgeOutput(in OutputDescription cp, IInlay patchInstance, object? innerValue)
    {
        _outputs[cp] = innerValue;
    }

    void IRegion<IInlay>.RetrieveInput(in InputDescription cp, IInlay patchInstance, out object? innerValue)
    {
        _inputs.TryGetValue(cp, out innerValue);
    }

    void IRegion<IInlay>.RetrieveOutput(in OutputDescription cp, out object? outerValue)
    {
        _outputs.TryGetValue(cp, out outerValue);
    }

    public interface IInlay
    {
        void Then();
        void Else();
    }
}

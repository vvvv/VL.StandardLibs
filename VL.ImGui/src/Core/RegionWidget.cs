using VL.Core.PublicAPI;

namespace VL.ImGui.Internal;

//[ProcessNode]
//[Region(SupportedBorderControlPoints = ControlPointType.Border)]
public abstract class RegionWidget : Widget, IRegion<RegionWidget.IInlay>, IDisposable
{
    private readonly Dictionary<string, object?> inputs = new();
    private readonly Dictionary<string, object?> outputs = new();
    private Func<IInlay>? patchInlayFactory;
    private IInlay? patchInlay;

    protected void ResetOutputs() => outputs.Clear();

    protected void RunPatchInlay(Context context)
    {
        if (patchInlay is null)
            patchInlay = patchInlayFactory?.Invoke();

        patchInlay?.Update(context);
    }

    protected void DisposePatchInlay()
    {
        var patchInlay = Interlocked.Exchange(ref this.patchInlay, null);
        if (patchInlay is IDisposable disposable)
            disposable.Dispose();
    }

    void IRegion<IInlay>.AcknowledgeInput(in InputDescription description, object? outerValue)
    {
        inputs[description.Id] = outerValue;
    }

    void IRegion<IInlay>.AcknowledgeOutput(in OutputDescription description, IInlay patchInlay, object? innerValue)
    {
        outputs[description.Id] = innerValue;
    }

    void IRegion<IInlay>.RetrieveInput(in InputDescription description, IInlay patchInlay, out object? innerValue)
    {
        inputs.TryGetValue(description.Id, out innerValue);
    }

    void IRegion<IInlay>.RetrieveOutput(in OutputDescription description, out object? outerValue)
    {
        outputs.TryGetValue(description.Id, out outerValue);
    }

    void IRegion<IInlay>.SetPatchInlayFactory(Func<IInlay> patchInlayFactory)
    {
        this.patchInlayFactory = patchInlayFactory;
    }

    void IDisposable.Dispose()
    {
        DisposePatchInlay();
    }

    public interface IInlay
    {
        void Update(Context context);
    }
}

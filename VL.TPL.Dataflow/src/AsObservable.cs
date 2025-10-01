using System.Reactive.Linq;

namespace VL.TPL.Dataflow;

[ProcessNode(Name = "AsObservable")]
public sealed class AsObservable<T>
{
    private IObservable<T> observable = Observable.Empty<T>();
    private ISourceBlock<T>? sourceBlock;

    public IObservable<T> Update(ISourceBlock<T>? input)
    {
        if (input != sourceBlock)
        {
            sourceBlock = input;
            observable = input?.AsObservable() ?? Observable.Empty<T>();
        }
        return observable;
    }
}

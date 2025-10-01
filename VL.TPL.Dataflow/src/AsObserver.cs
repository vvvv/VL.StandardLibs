using System.Reactive;

namespace VL.TPL.Dataflow;

[ProcessNode(Name = "AsObserver")]
public sealed class AsObserver<T>
{
    private IObserver<T> observer = Observer.Create<T>(_ => { });
    private ITargetBlock<T>? block;

    public IObserver<T> Update(ITargetBlock<T>? input)
    {
        if (input != block)
        {
            block = input;
            observer = input?.AsObserver() ?? Observer.Create<T>(_ => { });
        }
        return observer;
    }
}

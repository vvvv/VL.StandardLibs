using Microsoft.Extensions.Logging;
using VL.Core;

namespace VL.TPL.Dataflow;

/// <summary>Provides a dataflow block that invokes a provided <see cref="Action{T}"/> delegate for every data element received.</summary>
/// <typeparam name="T">Specifies the type of data operated on by this <see cref="ActionBlock{T}"/>.</typeparam>
[ProcessNode(Name = "ActionBlock")]
public class ActionBlockNode<T> : BlockNode<ActionBlock<T>, ExecutionDataflowBlockOptions, T>
{
    private CreateHandler? _create;
    private UpdateHandler<T>? _update;

    public ActionBlockNode([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    : base(nodeContext)
    {
    }

    [return: Pin(Name = "Output")]
    public ActionBlock<T> Update(
        CreateHandler create,
        UpdateHandler<T> update,
        ISourceBlock<T>? sourceBlock,
        ExecutionDataflowBlockOptions? options)
    {
        _create = create;
        _update = update;

        return Update(sourceBlock, options);
    }

    protected override ActionBlock<T> CreateBlock(ExecutionDataflowBlockOptions? options)
    {
        Debug.Assert(_create != null);
        Debug.Assert(_update != null);

        var manager = new StateManager<object>();
        var block = new ActionBlock<T>(
            action: x =>
            {
                try
                {
                    using var lease = manager.LeaseState(_create);
                    _update(lease.State, x, out lease.State);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    RuntimeGraph.ReportException(e, AppHost);
                    throw;
                }
            },
            dataflowBlockOptions: options ?? new());
        block.Completion.ContinueWith(_ => manager.Dispose(), options?.TaskScheduler ?? TaskScheduler.Default);
        return block;
    }
}
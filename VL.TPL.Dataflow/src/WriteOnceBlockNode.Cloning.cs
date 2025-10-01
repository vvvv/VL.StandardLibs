using Microsoft.Extensions.Logging;
using VL.Core;

namespace VL.TPL.Dataflow;

/// <summary>Provides a buffer for receiving and storing at most one element in a network of dataflow blocks.</summary>
/// <typeparam name="T">Specifies the type of the data buffered by this dataflow block.</typeparam>
[ProcessNode(Name = "WriteOnceBlock (Cloning)")]
public class CloningWriteOnceBlockNode<T> : BlockNode<WriteOnceBlock<T>, DataflowBlockOptions, T>
{
    private CreateHandler? _create;
    private UpdateHandler<T, T>? _update;

    public CloningWriteOnceBlockNode([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    : base(nodeContext)
    {
    }

    [return: Pin(Name = "Output")]
    public WriteOnceBlock<T> Update(
        CreateHandler create,
        UpdateHandler<T, T> update,
        ISourceBlock<T>? sourceBlock,
        DataflowBlockOptions? options = null)
    {
        _create = create;
        _update = update;

        return Update(sourceBlock, options);
    }

    protected override WriteOnceBlock<T> CreateBlock(DataflowBlockOptions? options)
    {
        Debug.Assert(_create != null);
        Debug.Assert(_update != null);

        var manager = new StateManager<object>();
        var block = new WriteOnceBlock<T>(
            cloningFunction: x =>
            {
                try
                {
                    using var lease = manager.LeaseState(_create);
                    _update(lease.State, x, out lease.State, out var output);
                    return output;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    RuntimeGraph.ReportException(e, AppHost);
                    throw;
                }
            },
            dataflowBlockOptions: options ?? new());

        block.Completion.ContinueWith(_ => manager.Dispose());

        return block;
    }
}
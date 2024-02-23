using Microsoft.Extensions.Logging;
using VL.Core;

namespace VL.TPL.Dataflow;

/// <summary>
/// Provides a buffer for storing at most one element at time, overwriting each message with the next as it arrives.
/// Messages are broadcast to all linked targets, all of which may consume a clone of the message.
/// </summary>
/// <typeparam name="T">Specifies the type of the data buffered by this dataflow block.</typeparam>
/// <remarks>
/// <see cref="BroadcastBlock{T}"/> exposes at most one element at a time.  However, unlike
/// <see cref="WriteOnceBlock{T}"/>, that element will be overwritten as new elements are provided
/// to the block.  <see cref="BroadcastBlock{T}"/> ensures that the current element is broadcast to any
/// linked targets before allowing the element to be overwritten.
/// </remarks>
[ProcessNode(Name = "BroadcastBlock (Cloning)")]
public class CloningBroadcastBlockNode<T> : BlockNode<BroadcastBlock<T>, DataflowBlockOptions, T>
{
    private CreateHandler? _create;
    private UpdateHandler<T, T>? _update;

    public CloningBroadcastBlockNode([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    : base(nodeContext)
    {
    }

    [return: Pin(Name = "Output")]
    public BroadcastBlock<T> Update(
        CreateHandler create,
        UpdateHandler<T, T> update,
        ISourceBlock<T>? sourceBlock,
        DataflowBlockOptions? options = null)
    {
        _create = create;
        _update = update;

        return Update(sourceBlock, options);
    }

    protected override BroadcastBlock<T> CreateBlock(DataflowBlockOptions? options)
    {
        Debug.Assert(_create != null);
        Debug.Assert(_update != null);

        var manager = new StateManager<object>();
        var block = new BroadcastBlock<T>(
            cloningFunction: x =>
            {
                try
                {
                    using var lease = manager.LeaseState(_create);
                    _update(lease.State, x, out lease.State, out var output);
                    return output;
                }
                catch (Exception e)
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
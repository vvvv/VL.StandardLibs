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
[ProcessNode(Name = "BroadcastBlock")]
public class BroadcastBlockNode<T> : BlockNode<BroadcastBlock<T>, DataflowBlockOptions, T>
{
    public BroadcastBlockNode([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    : base(nodeContext)
    {
    }

    protected override BroadcastBlock<T> CreateBlock(DataflowBlockOptions? options)
    {
        return new BroadcastBlock<T>(
                cloningFunction: null,
                dataflowBlockOptions: options ?? new());
    }
}
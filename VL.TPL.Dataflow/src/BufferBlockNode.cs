using VL.Core;

namespace VL.TPL.Dataflow;

/// <summary>Provides a buffer for storing data.</summary>
/// <typeparam name="T">Specifies the type of the data buffered by this dataflow block.</typeparam>
[ProcessNode(Name = "BufferBlock")]
public class BufferBlockNode<T> : BlockNode<BufferBlock<T>, DataflowBlockOptions, T>
{
    public BufferBlockNode([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    : base(nodeContext)
    {
    }

    protected override BufferBlock<T> CreateBlock(DataflowBlockOptions? options)
    {
        return new BufferBlock<T>(dataflowBlockOptions: options ?? new());
    }
}
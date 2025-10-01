using VL.Core;

namespace VL.TPL.Dataflow;

/// <summary>Provides a dataflow block that batches inputs into arrays.</summary>
/// <typeparam name="T">Specifies the type of data put into batches.</typeparam>
[ProcessNode(Name = "BatchBlock")]
public class BatchBlockNode<T> : BlockNode<BatchBlock<T>, GroupingDataflowBlockOptions, T>
{
    private int _batchSize;

    public BatchBlockNode([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    : base(nodeContext)
    {
    }

    [return: Pin(Name = "Output")]
    public BatchBlock<T> Update(ISourceBlock<T>? sourceBlock, GroupingDataflowBlockOptions? options, int batchSize = 1)
    {
        if (batchSize != _batchSize)
        {
            _batchSize = batchSize;
            StopCurrent();
        }

        return Update(sourceBlock, options);
    }

    protected override BatchBlock<T> CreateBlock(GroupingDataflowBlockOptions? options)
    {
        return new BatchBlock<T>(
                Math.Clamp(_batchSize, 1, int.MaxValue),
                dataflowBlockOptions: options ?? new());
    }
}
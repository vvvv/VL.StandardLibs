namespace VL.TPL.Dataflow;

/// <summary>
/// Provides a dataflow block that batches a specified number of inputs of potentially differing types
/// provided to one or more of its targets.
/// </summary>
/// <typeparam name="T1">Specifies the type of data accepted by the block's first target.</typeparam>
/// <typeparam name="T2">Specifies the type of data accepted by the block's second target.</typeparam>
/// <typeparam name="T3">Specifies the type of data accepted by the block's third target.</typeparam>
[ProcessNode(Name = "BatchedJoinBlock (3 Items)")]
public class BatchedJoinBlockNode<T1, T2, T3> : IDisposable
{
    private readonly SerialDisposable _link1 = new();
    private readonly SerialDisposable _link2 = new();
    private readonly SerialDisposable _link3 = new();

    private int _batchSize;
    private object? _source1, _source2, _source3;
    private BatchedJoinBlock<T1, T2, T3>? _block;
    private GroupingDataflowBlockOptions? _options;

    [return: Pin(Name = "Output")]
    public BatchedJoinBlock<T1, T2, T3> Update(ISourceBlock<T1>? source1, ISourceBlock<T2>? source2, ISourceBlock<T3>? source3, GroupingDataflowBlockOptions? options, int batchSize = 1)
    {
        if (_block is null || options != _options || batchSize != _batchSize)
        {
            _options = options;
            _batchSize = batchSize;

            _link1.Disposable = null;
            _link2.Disposable = null;
            _link3.Disposable = null;
            _block?.Complete();

            _block = new BatchedJoinBlock<T1, T2, T3>(Math.Clamp(batchSize, 1, int.MaxValue), options ?? new());
        }

        if (source1 != _source1)
        { 
            _source1 = source1;
            _link1.Disposable = null;
            _link1.Disposable = source1?.LinkTo(_block.Target1);
        }

        if (source2 != _source2)
        {
            _source2 = source2;
            _link2.Disposable = null;
            _link2.Disposable = source2?.LinkTo(_block.Target2);
        }

        if (source3 != _source3)
        {
            _source3 = source3;
            _link3.Disposable = null;
            _link3.Disposable = source3?.LinkTo(_block.Target3);
        }

        return _block;
    }

    public void Dispose()
    {
        _link1.Dispose();
        _link2.Dispose();
        _link3.Dispose();
        _block?.Complete();
    }
}
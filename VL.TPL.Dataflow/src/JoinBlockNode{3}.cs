namespace VL.TPL.Dataflow;

/// <summary>
/// Provides a dataflow block that joins across multiple dataflow sources, not necessarily of the same type,
/// waiting for one item to arrive for each type before they?re all released together as a tuple of one item per type.
/// </summary>
/// <typeparam name="T1">Specifies the type of data accepted by the block's first target.</typeparam>
/// <typeparam name="T2">Specifies the type of data accepted by the block's second target.</typeparam>
/// <typeparam name="T3">Specifies the type of data accepted by the block's third target.</typeparam>
[ProcessNode(Name = "JoinBlock (3 Items)")]
public class JoinBlockNode<T1, T2, T3> : IDisposable
{
    private readonly SerialDisposable _link1 = new();
    private readonly SerialDisposable _link2 = new();
    private readonly SerialDisposable _link3 = new();

    private object? _source1, _source2, _source3;
    private JoinBlock<T1, T2, T3>? _block;
    private GroupingDataflowBlockOptions? _options;

    [return: Pin(Name = "Output")]
    public JoinBlock<T1, T2, T3> Update(ISourceBlock<T1>? source1, ISourceBlock<T2>? source2, ISourceBlock<T3>? source3, GroupingDataflowBlockOptions? options)
    {
        if (_block is null || options != _options)
        {
            _options = options;

            _link1.Disposable = null;
            _link2.Disposable = null;
            _link3.Disposable = null;
            _block?.Complete();

            _block = new JoinBlock<T1, T2, T3>(options ?? new());
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
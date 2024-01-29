using Microsoft.Extensions.Logging;
using VL.Core;

namespace VL.TPL.Dataflow.Internal;

[ProcessNode]
public abstract class BlockNode<TBlock, TOptions, T> : IDisposable
    where TBlock : class, ITargetBlock<T>
    where TOptions : DataflowBlockOptions
{
    private readonly SerialDisposable _linkManager = new();
    private readonly AppHost _appHost;
    private readonly ILogger _logger;

    private ISourceBlock<T>? _sourceBlock;
    private ITargetBlock<T>? _targetBlock;

    private TOptions? _options;
    private TBlock? _block;

    public BlockNode([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    {
        _appHost = nodeContext.AppHost;
        _logger = nodeContext.GetLogger();
    }

    protected ILogger Logger => _logger;

    protected AppHost AppHost => _appHost;

    protected abstract TBlock CreateBlock(TOptions? options);

    [return: Pin(Name = "Output")]
    public TBlock Update(ISourceBlock<T>? sourceBlock, TOptions? options)
    {
        if (_block is null || options != _options)
        {
            _options = options;

            StopCurrent();

            _block = CreateBlock(_options);
        }

        if (sourceBlock != _sourceBlock || _block != _targetBlock)
        {
            _sourceBlock = sourceBlock;
            _targetBlock = _block;

            _linkManager.Disposable = null;
            _linkManager.Disposable = _sourceBlock?.LinkTo(_block);
        }

        return _block;
    }

    protected void StopCurrent()
    {
        _linkManager.Disposable = null;
        _block?.Complete();
        _block = null;
    }

    public void Dispose()
    {
        _linkManager.Dispose();
        StopCurrent();
    }
}
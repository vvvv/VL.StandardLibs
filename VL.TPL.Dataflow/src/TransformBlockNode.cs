﻿using Microsoft.Extensions.Logging;
using VL.Core;

namespace VL.TPL.Dataflow;

/// <summary>Provides a dataflow block that invokes a provided <see cref="System.Func{TInput,TOutput}"/> delegate for every data element received.</summary>
/// <typeparam name="TInput">Specifies the type of data received and operated on by this <see cref="TransformBlock{TInput,TOutput}"/>.</typeparam>
/// <typeparam name="TOutput">Specifies the type of data output by this <see cref="TransformBlock{TInput,TOutput}"/>.</typeparam>
[ProcessNode(Name = "TransformBlock")]
public class TransformBlockNode<TInput, TOutput> : BlockNode<TransformBlock<TInput, TOutput>, ExecutionDataflowBlockOptions, TInput>
{
    private CreateHandler? _create;
    private UpdateHandler<TInput, TOutput>? _update;

    public TransformBlockNode([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    : base(nodeContext)
    {
    }

    [return: Pin(Name = "Output")]
    public TransformBlock<TInput, TOutput> Update(
        CreateHandler create,
        UpdateHandler<TInput, TOutput> update,
        ISourceBlock<TInput>? sourceBlock,
        ExecutionDataflowBlockOptions? options = null)
    {
        _create = create;
        _update = update;

        return Update(sourceBlock, options);
    }

    protected override TransformBlock<TInput, TOutput> CreateBlock(ExecutionDataflowBlockOptions? options)
    {
        Debug.Assert(_create != null);
        Debug.Assert(_update != null);

        var manager = new StateManager<object>();
        var block = new TransformBlock<TInput, TOutput>(
            transform: x =>
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
        block.Completion.ContinueWith(_ => manager.Dispose(), options?.TaskScheduler ?? TaskScheduler.Default);
        return block;
    }
}
namespace VL.TPL.Dataflow;

[ProcessNode(Name = "ExecutionDataflowBlockOptions")]
public class ExecutionDataflowBlockOptionsNode
{
    private ExecutionDataflowBlockOptions _options = new();

    /// <param name="taskScheduler">The <see cref="TaskScheduler"/> to use for scheduling tasks.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <param name="maxMessagesPerTask">The maximum number of messages that may be processed per task.</param>
    /// <param name="boundedCapacity">The maximum number of messages that may be buffered by the block.</param>
    /// <param name="nameFormat">The format string to use when a block is queried for its name.</param>
    /// <param name="ensureOrdered">Whether ordered processing should be enforced on a block's handling of messages.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of messages that may be processed by the block concurrently.</param>
    /// <param name="singleProducerConstrained">Whether code using the dataflow block is constrained to one producer at a time.</param>
    [return: Pin(Name = "Output")]
    public ExecutionDataflowBlockOptions Update(
        TaskScheduler? taskScheduler = default,
        CancellationToken cancellationToken = default,
        int maxMessagesPerTask = DataflowBlockOptions.Unbounded,
        int boundedCapacity = DataflowBlockOptions.Unbounded,
        string nameFormat = "{0} Id={1}",
        bool ensureOrdered = true,
        int maxDegreeOfParallelism = 1,
        bool singleProducerConstrained = false)
    {
        if ((taskScheduler ?? TaskScheduler.Default) != _options.TaskScheduler ||
            cancellationToken != _options.CancellationToken ||
            maxMessagesPerTask != _options.MaxMessagesPerTask ||
            boundedCapacity != _options.BoundedCapacity ||
            nameFormat != _options.NameFormat ||
            ensureOrdered != _options.EnsureOrdered ||
            maxDegreeOfParallelism != _options.MaxDegreeOfParallelism ||
            singleProducerConstrained != _options.SingleProducerConstrained)
        {
            _options = new ExecutionDataflowBlockOptions
            {
                TaskScheduler = taskScheduler ?? TaskScheduler.Default,
                CancellationToken = cancellationToken,
                MaxMessagesPerTask = maxMessagesPerTask,
                BoundedCapacity = boundedCapacity,
                NameFormat = nameFormat,
                EnsureOrdered = ensureOrdered,
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                SingleProducerConstrained = singleProducerConstrained
            };
        }

        return _options;
    }
}

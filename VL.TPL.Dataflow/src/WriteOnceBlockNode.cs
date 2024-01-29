namespace VL.TPL.Dataflow;

/// <summary>Provides a buffer for receiving and storing at most one element in a network of dataflow blocks.</summary>
/// <typeparam name="T">Specifies the type of the data buffered by this dataflow block.</typeparam>
[ProcessNode(Name = "WriteOnceBlock")]
public class WriteOnceBlockNode<T> : BlockNode<WriteOnceBlock<T>, DataflowBlockOptions, T>
{
    [return: Pin(Name = "Output")]
    protected override WriteOnceBlock<T> CreateBlock(DataflowBlockOptions? options)
    {
        return new WriteOnceBlock<T>(
                cloningFunction: null,
                dataflowBlockOptions: options ?? new());
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using VL.Core.Import;
using VL.Lib.Collections;
using VL.Lib.IO;
using VL.Lib.Reactive;

[assembly: ImportType(typeof(ByteCollector), Category = "IO.Advanced")]
[assembly: ImportType(typeof(AsyncByteCollector), Name = $"{nameof(ByteCollector)} (Reactive)", Category = "IO.Advanced")]
[assembly: ImportType(typeof(CharCollector), Category = "IO.Advanced")]
[assembly: ImportType(typeof(AsyncCharCollector), Name = $"{nameof(CharCollector)} (Reactive)", Category = "IO.Advanced")]

namespace VL.Lib.IO
{
    /// <summary>
    /// Collects the incoming chunks into a spread.
    /// </summary>
    [ProcessNode]
    public class ByteCollector
    {
        Spread<byte> FOutput = Spread<byte>.Empty;

        public Spread<byte> Update(IEnumerable<Chunk<byte>> input, bool collect)
        {
            if (collect)
            {
                var builder = default(SpreadBuilder<byte>);
                foreach (var chunk in input)
                {
                    if (builder == null)
                        builder = new SpreadBuilder<byte>((int)Math.Min(chunk.TotalLength, int.MaxValue));
                    builder.AddRange(chunk.Data);
                }
                FOutput = builder?.ToSpread() ?? Spread<byte>.Empty;
            }
            return FOutput;
        }
    }

    /// <summary>
    /// Collects the incoming chunks into a string.
    /// </summary>
    [ProcessNode]
    public class CharCollector
    {
        string FOutput = string.Empty;

        public string Update(IEnumerable<Chunk<char>> input, bool collect)
        {
            if (collect)
            {
                var builder = default(StringBuilder);
                foreach (var chunk in input)
                {
                    if (builder == null)
                        builder = new StringBuilder((int)Math.Min(chunk.TotalLength, int.MaxValue));
                    builder.Append(chunk.Data);
                }
                FOutput = builder?.ToString() ?? string.Empty;
            }
            return FOutput;
        }
    }

    /// <summary>
    /// Collects the incoming chunks into spreads.
    /// </summary>
    [ProcessNode]
    public class AsyncByteCollector
    {
        object FInput;
        IObservable<Spread<byte>> FOutput = ObservableNodes.Never<Spread<byte>>();

        public IObservable<Spread<byte>> Update(IObservable<IObservable<Chunk<byte>>> input)
        {
            if (input != FInput)
            {
                FInput = input;
                FOutput = input.SelectMany(chunks => 
                    chunks.Aggregate(
                        seed: default(SpreadBuilder<byte>), 
                        accumulator: (builder, chunk) =>
                        {
                            if (builder == null)
                                builder = new SpreadBuilder<byte>((int)Math.Min(chunk.TotalLength, int.MaxValue));
                            builder.AddRange(chunk.Data);
                            return builder;
                        },
                        resultSelector: builder => builder?.ToSpread() ?? Spread<byte>.Empty));
            }
            return FOutput;
        }
    }

    /// <summary>
    /// Collects the incoming chunks into strings.
    /// </summary>
    [ProcessNode]
    public class AsyncCharCollector
    {
        object FInput;
        IObservable<string> FOutput = ObservableNodes.Never<string>();

        public IObservable<string> Update(IObservable<IObservable<Chunk<char>>> input)
        {
            if (input != FInput)
            {
                FInput = input;
                FOutput = input.SelectMany(chunks => 
                    chunks.Aggregate(
                        seed: default(StringBuilder),
                        accumulator: (builder, chunk) =>
                        {
                            if (builder == null)
                                builder = new StringBuilder((int)Math.Min(chunk.TotalLength, int.MaxValue));
                            builder.Append(chunk.Data);
                            return builder;
                        }, 
                        resultSelector: builder => builder?.ToString() ?? string.Empty));
            }
            return FOutput;
        }
    }
}

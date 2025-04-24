using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core.Import;
using VL.Lib.Collections;
using VL.Lib.IO;
using VL.Lib.Reactive;

[assembly: ImportType(typeof(ByteChunkifier), Category = "IO.Advanced")]
[assembly: ImportType(typeof(AsyncByteChunkifier), Name = $"{nameof(ByteChunkifier)} (Reactive)", Category = "IO.Advanced")]
[assembly: ImportType(typeof(CharChunkifier), Category = "IO.Advanced")]
[assembly: ImportType(typeof(AsyncCharChunkifier), Name = $"{nameof(CharChunkifier)} (Reactive)", Category = "IO.Advanced")]

namespace VL.Lib.IO
{
    /// <summary>
    /// Chunkifies the input spread.
    /// </summary>
    [ProcessNode]
    public class ByteChunkifier
    {
        Spread<byte> FInput;
        IEnumerable<Chunk<byte>> FOutput = Enumerable.Empty<Chunk<byte>>();

        public IEnumerable<Chunk<byte>> Update(Spread<byte> input)
        {
            if (input != FInput)
            {
                FInput = input;
                FOutput = CreateChunkifier(input);
            }
            return FOutput;
        }

        private IEnumerable<Chunk<byte>> CreateChunkifier(Spread<byte> data)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(StreamUtils.LargeBufferSize);
            try
            {
                var totalByteCount = data.Count;
                var totalCopiedBytes = 0;
                for (int i = 0; i < totalByteCount; i += buffer.Length)
                {
                    var bytesToCopy = Math.Min(buffer.Length, totalByteCount - i);
                    data.CopyTo(i, buffer, 0, bytesToCopy);
                    totalCopiedBytes += bytesToCopy;
                    var chunk = Chunk<byte>.Create(buffer, 0, bytesToCopy, totalCopiedBytes, totalByteCount);
                    yield return chunk;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    /// <summary>
    /// Chunkifies the input string.
    /// </summary>
    [ProcessNode]
    public class CharChunkifier
    {
        string FInput;
        IEnumerable<Chunk<char>> FOutput = Enumerable.Empty<Chunk<char>>();

        public IEnumerable<Chunk<char>> Update(string input)
        {
            if (input != FInput)
            {
                FInput = input;
                FOutput = CreateChunkifier(input);
            }
            return FOutput;
        }

        private IEnumerable<Chunk<char>> CreateChunkifier(string data)
        {
            var buffer = ArrayPool<char>.Shared.Rent(StreamUtils.LargeCharBufferSize);
            try
            {
                var totalCharCount = data.Length;
                var totalCopiedChars = 0;
                for (int i = 0; i < totalCharCount; i += buffer.Length)
                {
                    var charsToCopy = Math.Min(buffer.Length, totalCharCount - i);
                    data.CopyTo(i, buffer, 0, charsToCopy);
                    totalCopiedChars += charsToCopy;
                    var chunk = Chunk<char>.Create(buffer, 0, charsToCopy, totalCopiedChars, totalCharCount);
                    yield return chunk;
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }
    }

    /// <summary>
    /// Chunkifies the incoming spreads.
    /// </summary>
    [ProcessNode]
    public class AsyncByteChunkifier
    {
        object FInput;
        IObservable<IObservable<Chunk<byte>>> FOutput = ObservableNodes.Never<IObservable<Chunk<byte>>>();

        public IObservable<IObservable<Chunk<byte>>> Update(IObservable<Spread<byte>> input)
        {
            if (input != FInput)
            {
                FInput = input;
                FOutput = input.Select(data => 
                    Observable.Create<Chunk<byte>>(observer =>
                    {
                        var buffer = ArrayPool<byte>.Shared.Rent(StreamUtils.LargeBufferSize);
                        try
                        {
                            var totalByteCount = data.Count;
                            var totalCopiedBytes = 0;
                            for (int i = 0; i < totalByteCount; i += buffer.Length)
                            {
                                var bytesToCopy = Math.Min(buffer.Length, totalByteCount - i);
                                data.CopyTo(i, buffer, 0, bytesToCopy);
                                totalCopiedBytes += bytesToCopy;
                                var chunk = Chunk<byte>.Create(buffer, 0, bytesToCopy, totalCopiedBytes, totalByteCount);
                                observer.OnNext(chunk);
                            }
                            observer.OnCompleted();
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                        return Disposable.Empty;
                    }));
            }
            return FOutput;
        }
    }

    /// <summary>
    /// Chunkifies the incoming strings.
    /// </summary>
    [ProcessNode]
    public class AsyncCharChunkifier
    {
        object FInput;
        IObservable<IObservable<Chunk<char>>> FOutput = ObservableNodes.Never<IObservable<Chunk<char>>>();

        public IObservable<IObservable<Chunk<char>>> Update(IObservable<string> input)
        {
            if (input != FInput)
            {
                FInput = input;
                FOutput = input.Select(data => 
                    Observable.Create<Chunk<char>>(observer =>
                    {
                        var buffer = ArrayPool<char>.Shared.Rent(StreamUtils.LargeCharBufferSize);
                        try
                        {
                            var totalCharCount = data.Length;
                            var totalCopiedChars = 0;
                            for (int i = 0; i < totalCharCount; i += buffer.Length)
                            {
                                var charsToCopy = Math.Min(buffer.Length, totalCharCount - i);
                                data.CopyTo(i, buffer, 0, charsToCopy);
                                totalCopiedChars += charsToCopy;
                                var chunk = Chunk<char>.Create(buffer, 0, charsToCopy, totalCopiedChars, totalCharCount);
                                observer.OnNext(chunk);
                            }
                            observer.OnCompleted();
                        }
                        finally
                        {
                            ArrayPool<char>.Shared.Return(buffer);
                        }
                        return Disposable.Empty;
                    }));
            }
            return FOutput;
        }
    }
}

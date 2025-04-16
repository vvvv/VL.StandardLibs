using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Basics.Resources;
using VL.Lib.IO;
using VL.Lib.Reactive;
using VL.Lib.Text;

[assembly: ImportType(typeof(ByteReader), Category = "IO.Advanced")]
[assembly: ImportType(typeof(AsyncByteReader), Name = $"{nameof(ByteReader)} (Reactive)", Category = "IO.Advanced")]
[assembly: ImportType(typeof(CharReader), Category = "IO.Advanced")]
[assembly: ImportType(typeof(AsyncCharReader), Name = $"{nameof(CharReader)} (Reactive)", Category = "IO.Advanced")]
[assembly: ImportType(typeof(XDocumentReader), Category = "System.XML.Advanced")]
[assembly: ImportType(typeof(AsyncXDocumentReader), Name = $"{nameof(XDocumentReader)} (Reactive)", Category = "System.XML.Advanced")]

namespace VL.Lib.IO
{
    /// <summary>
    /// Returns a sequence which will read chunks of bytes from the given stream when enumerated.
    /// </summary>
    [ProcessNode]
    public class ByteReader
    {
        object FInput;
        IEnumerable<Chunk<byte>> FOutput = Enumerable.Empty<Chunk<byte>>();

        public ByteReader(NodeContext nodeContext)
        {
        }

        public IEnumerable<Chunk<byte>> Update(
            IResourceProvider<Stream> input)
        {
            if (input != FInput)
            {
                FInput = input;
                FOutput = CreateReader(input);
            }
            return FOutput;
        }

        IEnumerable<Chunk<byte>> CreateReader(IResourceProvider<Stream> provider)
        {
            using (var handle = provider.GetHandle())
            {
                var stream = handle.Resource;
                var bufferSize = stream is FileStream ? StreamUtils.LargeBufferSize : StreamUtils.SmallBufferSize;
                var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    var streamLength = stream.CanSeek ? stream.Length : long.MaxValue;
                    while (stream.CanRead)
                    {
                        var bytesRead = stream.Read(buffer, 0, bufferSize);
                        if (bytesRead > 0)
                        {
                            var chunk = Chunk<byte>.Create(buffer, 0, bytesRead, stream.Position, streamLength);
                            yield return chunk;
                        }
                        else
                            break;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
    }

    /// <summary>
    /// Returns a sequence which will read chunks of characters from the given stream when enumerated.
    /// </summary>
    [ProcessNode]
    public class CharReader
    {
        object FInput;
        Encodings FEncoding;
        IEnumerable<Chunk<char>> FOutput = Enumerable.Empty<Chunk<char>>();

        public CharReader(NodeContext nodeContext)
        {
        }

        public IEnumerable<Chunk<char>> Update(
            IResourceProvider<Stream> input, Encodings encoding)
        {
            if (input != FInput || encoding != FEncoding)
            {
                FInput = input;
                FEncoding = encoding;
                var enc = encoding.ToEncoding();
                FOutput = CreateReader(input, enc);
            }
            return FOutput;
        }

        IEnumerable<Chunk<char>> CreateReader(IResourceProvider<Stream> provider, Encoding encoding)
        {
            using (var handle = provider.GetHandle())
            {
                var stream = handle.Resource;
                var bufferSize = stream is FileStream ? StreamUtils.LargeBufferSize : StreamUtils.SmallBufferSize;
                using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: bufferSize, leaveOpen: true))
                {
                    var charBufferSize = stream is FileStream ? StreamUtils.LargeCharBufferSize : StreamUtils.SmallCharBufferSize;
                    var buffer = ArrayPool<char>.Shared.Rent(charBufferSize);
                    try
                    {
                        var streamLength = stream.CanSeek ? stream.Length : long.MaxValue;
                        while (!reader.EndOfStream)
                        {
                            var charsRead = reader.Read(buffer, 0, buffer.Length);
                            if (charsRead > 0)
                            {
                                var chunk = Chunk<char>.Create(buffer, 0, charsRead, stream.Position, streamLength);
                                yield return chunk;
                            }
                            else
                                break;
                        }
                    }
                    finally
                    {
                        ArrayPool<char>.Shared.Return(buffer);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Reads and returns the <see cref="XDocument"/> from the given stream.
    /// </summary>
    [ProcessNode]
    public class XDocumentReader
    {
        XDocument FOutput = new XDocument();

        public XDocumentReader(NodeContext nodeContext)
        {
        }

        public XDocument Update(
            IResourceProvider<Stream> input, bool read)
        {
            if (read)
                FOutput = Read(input);
            return FOutput;
        }

        XDocument Read(IResourceProvider<Stream> provider)
        {
            using (var handle = provider.GetHandle())
            {
                var stream = handle.Resource;
                return XDocument.Load(stream);
            }
        }
    }

    /// <summary>
    /// Returns an observable sequence which will read chunks of bytes from the given stream on subscription.
    /// </summary>
    [ProcessNode]
    public class AsyncByteReader
    {
        object FInput;
        IObservable<IObservable<Chunk<byte>>> FOutput = ObservableNodes.Never<IObservable<Chunk<byte>>>();

        public AsyncByteReader(NodeContext nodeContext)
        {

        }

        public IObservable<IObservable<Chunk<byte>>> Update(IObservable<IResourceProvider<Stream>> input)
        {
            if (input != FInput)
            {
                FInput = input;
                FOutput = input.Select(CreateReader);
            }
            return FOutput;
        }

        IObservable<Chunk<byte>> CreateReader(IResourceProvider<Stream> provider)
        {
            return Observable.Create<Chunk<byte>>(async (observer, token) =>
            {
                using (var handle = await provider.GetHandleAsync(token).ConfigureAwait(false))
                {
                    var stream = handle.Resource;
                    var bufferSize = stream is FileStream ? StreamUtils.LargeBufferSize : StreamUtils.SmallBufferSize;
                    var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                    try
                    {
                        var streamLength = stream.CanSeek ? stream.Length : long.MaxValue;
                        while (stream.CanRead && !token.IsCancellationRequested)
                        {
                            var bytesRead = await stream.ReadAsync(buffer, 0, bufferSize, token).ConfigureAwait(false);
                            if (bytesRead > 0)
                            {
                                var chunk = Chunk<byte>.Create(buffer, 0, bytesRead, stream.Position, streamLength);
                                observer.OnNext(chunk);
                            }
                            else
                                break;
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
                if (!token.IsCancellationRequested)
                    observer.OnCompleted();
            }).PubRefCount();
        }
    }

    /// <summary>
    /// Returns an observable sequence which will read chunks of characters from the given stream on subscription.
    /// </summary>
    [ProcessNode]
    public class AsyncCharReader
    {
        object FInput;
        Encodings FEncoding;
        IObservable<IObservable<Chunk<char>>> FOutput = ObservableNodes.Never<IObservable<Chunk<char>>>();

        public AsyncCharReader(NodeContext nodeContext)
        {
        }

        public IObservable<IObservable<Chunk<char>>> Update(
            IObservable<IResourceProvider<Stream>> input, Encodings encoding)
        {
            if (input != FInput || encoding != FEncoding)
            {
                FInput = input;
                FEncoding = encoding;
                var enc = encoding.ToEncoding();
                FOutput = input.Select(provider => CreateReader(provider, enc));
            }
            return FOutput;
        }

        IObservable<Chunk<char>> CreateReader(IResourceProvider<Stream> provider, Encoding encoding)
        {
            return Observable.Create<Chunk<char>>(async (observer, token) =>
            {
                using (var handle = await provider.GetHandleAsync(token).ConfigureAwait(false))
                {
                    var stream = handle.Resource;
                    var bufferSize = stream is FileStream ? StreamUtils.LargeBufferSize : StreamUtils.SmallBufferSize;
                    using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: bufferSize, leaveOpen: true))
                    {
                        var charBufferSize = stream is FileStream ? StreamUtils.LargeCharBufferSize : StreamUtils.SmallCharBufferSize;
                        var buffer = ArrayPool<char>.Shared.Rent(charBufferSize);
                        try
                        {
                            var streamLength = stream.CanSeek ? stream.Length : long.MaxValue;
                            while (!reader.EndOfStream && !token.IsCancellationRequested)
                            {
                                var charsRead = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                                if (charsRead > 0)
                                {
                                    var chunk = Chunk<char>.Create(buffer, 0, charsRead, stream.Position, streamLength);
                                    observer.OnNext(chunk);
                                }
                                else
                                    break;
                            }
                        }
                        finally
                        {
                            ArrayPool<char>.Shared.Return(buffer);
                        }
                    }
                }
                if (!token.IsCancellationRequested)
                    observer.OnCompleted();
            }).PubRefCount();
        }
    }

    /// <summary>
    /// Returns an observable sequence which will read the <see cref="XDocument"/> from the given stream on subscription.
    /// </summary>
    [ProcessNode]
    public class AsyncXDocumentReader
    {
        object FInput;
        IObservable<XDocument> FOutput = ObservableNodes.Never<XDocument>();
        bool FInProgress, FOnCompleted;

        public AsyncXDocumentReader(NodeContext nodeContext)
        {
        }

        public IObservable<XDocument> Update(IObservable<IResourceProvider<Stream>> input, out bool inProgress, out bool onCompleted)
        {
            if (input != FInput)
            {
                FInput = input;
                FOutput = input.SelectMany(CreateReader);
            }

            inProgress = FInProgress;
            onCompleted = FOnCompleted;
            if (onCompleted)
                FOnCompleted = false;

            return FOutput;
        }

        private IObservable<XDocument> CreateReader(IResourceProvider<Stream> provider)
        {
            return Observable.Create<XDocument>(async (observer, token) =>
            {
                FInProgress = true;
                try
                {
                    using (var handle = await provider.GetHandleAsync(token).ConfigureAwait(false))
                    {
                        var stream = handle.Resource;
                        var document = XDocument.Load(stream);
                        observer.OnNext(document);
                    }
                    observer.OnCompleted();
                    FOnCompleted = true;
                }
                catch (OperationCanceledException)
                {
                    // Fine
                }
                finally
                {
                    FInProgress = false;
                }
            }).PubRefCount();
        }
    }
}

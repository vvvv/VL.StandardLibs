using System;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.Reactive;
using VL.Lib.Text;
using System.Threading;
using System.Collections.Generic;
using VL.Core.Import;
using VL.Lib.IO;

[assembly: ImportType(typeof(ByteWriter), Category = "IO.Advanced")]
[assembly: ImportType(typeof(AsyncByteWriter), Name = $"{nameof(ByteWriter)} (Reactive)", Category = "IO.Advanced")]
[assembly: ImportType(typeof(CharWriter), Category = "IO.Advanced")]
[assembly: ImportType(typeof(AsyncCharWriter), Name = $"{nameof(CharWriter)} (Reactive)", Category = "IO.Advanced")]
[assembly: ImportType(typeof(XDocumentWriter), Category = "System.XML.Advanced")]
[assembly: ImportType(typeof(AsyncXDocumentWriter), Name = $"{nameof(XDocumentWriter)} (Reactive)", Category = "System.XML.Advanced")]

namespace VL.Lib.IO
{
    /// <summary>
    /// Writes the incoming chunks of bytes to the given stream.
    /// </summary>
    [ProcessNode]
    public class ByteWriter
    {
        public ByteWriter(NodeContext nodeContext)
        {
        }

        public void Update(IResourceProvider<Stream> input, IEnumerable<Chunk<byte>> data, bool write)
        {
            if (write)
                Write(input, data);
        }

        void Write(IResourceProvider<Stream> provider, IEnumerable<Chunk<byte>> chunks)
        {
            using (var handle = provider.GetHandle())
            {
                var stream = handle.Resource;
                foreach (var chunk in chunks)
                {
                    var data = chunk.Data;
                    stream.Write(data, 0, data.Length);
                }
            }
        }
    }

    /// <summary>
    /// Writes the incoming chunks of characters to the given stream.
    /// </summary>
    [ProcessNode]
    public class CharWriter
    {
        public CharWriter(NodeContext nodeContext)
        {
        }

        public void Update(IResourceProvider<Stream> input, IEnumerable<Chunk<char>> data, Encodings encoding, bool write)
        {
            if (write)
                Write(input, data, encoding.ToEncoding());
        }

        void Write(IResourceProvider<Stream> provider, IEnumerable<Chunk<char>> chunks, Encoding encoding)
        {
            using (var handle = provider.GetHandle())
            {
                var stream = handle.Resource;
                var bufferSize = stream is FileStream ? StreamUtils.LargeBufferSize : StreamUtils.SmallBufferSize;
                using (var streamWriter = new StreamWriter(stream, encoding, bufferSize, leaveOpen: true))
                {
                    foreach (var chunk in chunks)
                    {
                        var data = chunk.Data;
                        streamWriter.Write(data, 0, data.Length);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Writes the <see cref="XDocument"/> to the given stream.
    /// </summary>
    [ProcessNode]
    public class XDocumentWriter
    {
        public XDocumentWriter(NodeContext nodeContext)
        {
        }

        public void Update(IResourceProvider<Stream> input, XDocument data, bool write)
        {
            if (write)
                Write(input, data);
        }

        void Write(IResourceProvider<Stream> provider, XDocument document)
        {
            using var handle = provider.GetHandle();
            var stream = handle.Resource;
            using var writer = new StreamWriter(stream, Encodings.UTF8.ToEncoding());
            document.Save(writer);
        }
    }

    /// <summary>
    /// Returns an observable sequence which will write the incoming chunks of bytes to the given stream on subscription.
    /// </summary>
    [ProcessNode]
    public class AsyncByteWriter
    {
        IObservable<IObservable<Chunk<byte>>> FOutput = ObservableNodes.Never<IObservable<Chunk<byte>>>();
        object FInput, FData;

        public AsyncByteWriter(NodeContext nodeContext) { }

        public IObservable<IObservable<Chunk<byte>>> Update(
            IResourceProvider<Stream> input, IObservable<IObservable<Chunk<byte>>> data)
        {
            if (input != FInput || data != FData)
            {
                FInput = input;
                FData = data;
                FOutput = data.Select(chunks => CreateWriter(input, chunks));
            }
            return FOutput;
        }

        IObservable<Chunk<byte>> CreateWriter(IResourceProvider<Stream> provider, IObservable<Chunk<byte>> chunks)
        {
            return Observable.Create<Chunk<byte>>(async (observer, token) =>
            {
                using (var handle = await provider.GetHandleAsync(token))
                {
                    var stream = handle.Resource;
                    await chunks.ForEachAsync(chunk =>
                    {
                        var data = chunk.Data;
                        stream.Write(data, 0, data.Length);
                        observer.OnNext(chunk);
                    });

                    // Doesn't work - threads get interleaved
                    //await chunks.SelectMany(async (chunk, innerToken) =>
                    //{
                    //    var data = chunk.Data;
                    //    await stream.WriteAsync(data, 0, data.Length, innerToken);
                    //    observer.OnNext(chunk);
                    //    return chunk;
                    //});
                }
                if (!token.IsCancellationRequested)
                    observer.OnCompleted();
            }).PubRefCount();
        }
    }

    /// <summary>
    /// Returns an observable sequence which will write the incoming chunks of characters to the given stream on subscription.
    /// </summary>
    [ProcessNode]
    public class AsyncCharWriter
    {
        IObservable<IObservable<Chunk<char>>> FOutput = ObservableNodes.Never<IObservable<Chunk<char>>>();
        object FInput, FData;
        Encodings FEncoding;

        public AsyncCharWriter(NodeContext nodeContext) { }

        public IObservable<IObservable<Chunk<char>>> Update(
            IResourceProvider<Stream> input, IObservable<IObservable<Chunk<char>>> data, Encodings encoding)
        {
            if (input != FInput || data != FData || encoding != FEncoding)
            {
                FInput = input;
                FData = data;
                FEncoding = encoding;
                var enc = encoding.ToEncoding();
                FOutput = data.Select(chunks => CreateWriter(input, chunks, enc));
            }
            return FOutput;
        }

        IObservable<Chunk<char>> CreateWriter(IResourceProvider<Stream> provider, IObservable<Chunk<char>> chunks, Encoding encoding)
        {
            return Observable.Create<Chunk<char>>(async (observer, token) =>
            {
                using (var handle = await provider.GetHandleAsync(token))
                {
                    var stream = handle.Resource;
                    var bufferSize = stream is FileStream ? StreamUtils.LargeBufferSize : StreamUtils.SmallBufferSize;
                    using (var streamWriter = new StreamWriter(stream, encoding, bufferSize, leaveOpen: true))
                    {
                        await chunks.ForEachAsync(chunk =>
                        {
                            var data = chunk.Data;
                            streamWriter.Write(data, 0, data.Length);
                            observer.OnNext(chunk);
                        });

                        // Doesn't work - threads get interleaved and data scrambled
                        //await chunks.SelectMany(async (Chunk<char> chunk, CancellationToken innerToken) =>
                        //{
                        //    var data = chunk.Data;
                        //    await streamWriter.WriteAsync(data, 0, data.Length);
                        //    observer.OnNext(chunk);
                        //    return chunk;
                        //});
                    }
                }

                if (!token.IsCancellationRequested)
                    observer.OnCompleted();
            }).PubRefCount();
        }
    }

    /// <summary>
    /// Returns an observable sequence which will start writing the document to the given stream on subscription.
    /// </summary>
    [ProcessNode]
    public class AsyncXDocumentWriter
    {
        IObservable<XDocument> FOutput = ObservableNodes.Never<XDocument>();
        object FInput, FData;
        bool FInProgress, FOnCompleted;

        public AsyncXDocumentWriter(NodeContext nodeContext)
        {
        }

        public IObservable<XDocument> Update(IResourceProvider<Stream> input, IObservable<XDocument> data, out bool inProgress, out bool onCompleted)
        {
            if (input != FInput || data != FData)
            {
                FInput = input;
                FData = data;
                FOutput = data.SelectMany(document => CreateWriter(input, document));
            }

            inProgress = FInProgress;
            onCompleted = FOnCompleted;
            if (onCompleted)
                FOnCompleted = onCompleted;

            return FOutput;
        }

        private IObservable<XDocument> CreateWriter(IResourceProvider<Stream> provider, XDocument document)
        {
            return Observable.Create<XDocument>(async (observer, token) =>
            {
                FInProgress = true;
                try
                {
                    using (var handle = await provider.GetHandleAsync(token).ConfigureAwait(false))
                    {
                        var stream = handle.Resource;
                        document.Save(stream);
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

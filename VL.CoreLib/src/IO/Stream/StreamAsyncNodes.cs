using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;
using VL.Lib.IO;
using VL.Lib.Text;

[assembly: ImportType(typeof(ReaderNode), Name = "Reader", Category = "IO.Experimental.Stream")]
[assembly: ImportType(typeof(ReaderAll), Name = "ReaderAll", Category = "IO.Experimental.Stream")]
[assembly: ImportType(typeof(ReaderString), Name = "ReaderAll", Category = "IO.Experimental.Stream")]
[assembly: ImportType(typeof(WriterBytes), Name = "Writer", Category = "IO.Experimental.Stream")]
[assembly: ImportType(typeof(WriterString), Name = "Writer (String)", Category = "IO.Experimental.Stream")]

namespace VL.Lib.IO
{
    public class Reader<T> : IDisposable
    {
        private IResourceProvider<Stream> FStreamProvider;
        private long FOffset;
        private long FCount;

        protected T FEmpty;
        protected T FData;
        private Task FLastTask;
        private CancellationTokenSource FLastCancellationTokenSource;
        private float FProgress;

        protected Reader()
        {
            FOffset = -10;
            FCount = -10;

            FProgress = 0.0f;
        }

        public void Dispose()
        {
            if (FLastCancellationTokenSource != null)
                FLastCancellationTokenSource.Cancel();
        }

        protected IResourceProvider<Stream> Read(IResourceProvider<Stream> input, long offset, long count, bool @do, bool abort, out float progress, out bool inProgress, out T data, 
            Func<IResourceProvider<Stream>, long, long, IProgress<float>, CancellationToken, Task<T>> readAsyncTask)
        {
            if (FStreamProvider != input) //input resource provider changed - abort
            {
                FStreamProvider = input;
                abort = true;
            }

            abort |= (FOffset != offset || FCount != count  || @do); //other input params changed - abort

            if (FLastCancellationTokenSource != null && abort) //cancel task if running
                FLastCancellationTokenSource.Cancel();

            if (FLastTask != null && FLastTask.IsFaulted)
            {
                var e = FLastTask.Exception.InnerException;
                FLastTask = null; //clear this, to throw the exception only once
                while (e.InnerException != null)
                    e = e.InnerException;
                if (!(e is TaskCanceledException))
                    throw e;
            }

            if (@do)
            {
                FOffset = offset;
                FCount = count;

                var cts = new CancellationTokenSource();
                FLastCancellationTokenSource = cts;
                FData = FEmpty;
                FProgress = 0.0f;
                var p = new Progress<float>(x => { if (!cts.IsCancellationRequested) { FProgress = x; } });

                FLastTask = Task.Run(() => readAsyncTask(input, offset, count, p, cts.Token), cts.Token)
                    .ContinueWith(a => EndRead(a));
            }
            progress = FProgress;
            data = FData;
            inProgress = (FLastTask != null && (FLastTask.Status == TaskStatus.Running || FLastTask.Status == TaskStatus.WaitingForActivation));

            return FStreamProvider;
        }

        private void EndRead(Task<T> antecedent)
        {
            if (antecedent.IsCompleted)
                FData = antecedent.Result;
        }
    }

    public class ReaderBytes : Reader<Spread<byte>>
    {
        public ReaderBytes()
        {
            FEmpty = Spread<byte>.Empty;
            FData = Spread<byte>.Empty;
        }

        protected async Task<Spread<byte>> ReadBytesTask(IResourceProvider<Stream> input, long offset, long count, IProgress<float> progress, CancellationToken ct)
        {
           var builder = new SpreadBuilder<byte>();
            using (var handle = input.GetHandle())
            {
                if (count == 0)
                    return FEmpty;

                var stream = handle.Resource;
                if (stream.CanSeek)
                {
                    stream.Position += offset;

                    if (count == long.MaxValue)
                        count = stream.Length;
                }

                var buffer = StreamUtils.LargeBuffer;
                var bytesToRead = count;
                var bytesRead = 0L;
                var st = Stopwatch.StartNew();
                while (bytesRead < bytesToRead && !ct.IsCancellationRequested)
                {
                    var chunkSize = (int)Math.Min(buffer.Length, bytesToRead);
                    var readCount = await stream.ReadAsync(buffer, 0, chunkSize);
                    if (readCount == 0)
                        break;
                    builder.AddRange(buffer, readCount);
                    bytesRead += readCount;

                    // Report every 40ms (25 fps)
                    if (st.ElapsedMilliseconds >= 40)
                    {
                        progress.Report(bytesRead / (float)bytesToRead);
                        st.Restart();
                    }
                }

                // Report the final value
                progress.Report(1f);
            }
            return builder.ToSpread();
        }
    }

    [ProcessNode]
    public class ReaderNode : ReaderBytes
    {
        public IResourceProvider<Stream> Update(IResourceProvider<Stream> input, long offset, long count, bool read, bool abort, out Spread<byte> data, out float progress, out bool inProgress)
        {
            return Read(input, offset, count, read, abort, out progress, out inProgress, out data, ReadBytesTask);
        }
    }

    [ProcessNode]
    public class ReaderAll : ReaderBytes
    {
        public IResourceProvider<Stream> Update(IResourceProvider<Stream> input, long offset, bool read, bool abort, out Spread<byte> data, out float progress, out bool inProgress)
        {
            return Read(input, offset, long.MaxValue, read, abort, out progress, out inProgress, out data, ReadBytesTask);
        }
    }

    /// <summary>
    /// Asynchronously reads string from an entire stream
    /// </summary>
    [ProcessNode]
    public class ReaderString : Reader<string>
    {
        public ReaderString()
        {
            FEmpty = string.Empty;
            FData = string.Empty;
        }

        private async Task<string> ReadStringTask(IResourceProvider<Stream> input, long offset, long count, IProgress<float> progress, CancellationToken ct, Encodings encoding)
        {
            SpreadBuilder<byte> builder = new SpreadBuilder<byte>();
            using (var handle = input.GetHandle())
            {
                if (count == 0)
                    return FEmpty;

                var stream = handle.Resource;
                if (stream.CanSeek)
                {
                    stream.Position += offset;

                    if (count == long.MaxValue)
                        count = stream.Length;
                }

                long bytesToRead = count;
                int bytesRead = 0;
                var buffer = StreamUtils.SmallBuffer;
                do
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, (int)Math.Min(StreamUtils.SmallBufferSize, bytesToRead));
                    builder.AddRange(buffer, bytesRead);
                    bytesToRead -= bytesRead;

                    progress.Report(1.0f - (bytesToRead / (float)count));
                } while (bytesRead > 0 && bytesToRead > 0 && !ct.IsCancellationRequested);
                ct.ThrowIfCancellationRequested();
            }
            return encoding.ToEncoding().GetString(builder.ToArray());
        }

        public IResourceProvider<Stream> ReadAllString(IResourceProvider<Stream> input, Encodings encoding, long offset, bool read, bool abort, out float progress, out bool inProgress, out string data)
        {
            return Read(input, offset, long.MaxValue, read, abort, out progress, out inProgress, out data, (p, a, b, c, d) => ReadStringTask(p, a, b, c, d, encoding));
        }
    }

    public class Writer<T> : IDisposable
    {
        private IResourceProvider<Stream> FStreamProvider;
        private long FOffset;

        private Task FLastTask;
        private CancellationTokenSource FLastCancellationTokenSource;
        private float FProgress;


        public Writer()
        {
            FOffset = -10;

            FProgress = 0.0f;
        }

        public void Dispose()
        {
            if (FLastCancellationTokenSource != null)
                FLastCancellationTokenSource.Cancel();
        }

        protected IResourceProvider<Stream> Write(IResourceProvider<Stream> input, T data, long offset, bool @do, bool abort, out float progress, out bool inProgress,
            Func<IResourceProvider<Stream>, T, long, IProgress<float>, CancellationToken, Task> writeAsyncTask)
        {
            if (FStreamProvider != input) //input resource provider changed - abort
            {
                FStreamProvider = input;
                abort = true;
            }

            abort |= (FOffset != offset || @do); //other input params changed - abort

            if (FLastCancellationTokenSource != null && abort) //cancel task if running
                FLastCancellationTokenSource.Cancel();

            if (FLastTask != null && FLastTask.IsFaulted)
            {
                var e = FLastTask.Exception.InnerException;
                FLastTask = null; //clear this, to throw the exception only once
                while (e.InnerException != null)
                    e = e.InnerException;
                if (!(e is TaskCanceledException))
                    throw e;
            }

            if (@do)
            {
                FOffset = offset;

                var cts = new CancellationTokenSource();
                FLastCancellationTokenSource = cts;
                FProgress = 0.0f;
                var p = new Progress<float>(x => { if (!cts.IsCancellationRequested) { FProgress = x; } });

                FLastTask = Task.Run(() => writeAsyncTask(input, data, offset, p, cts.Token), cts.Token);
            }
            progress = FProgress;
            inProgress = (FLastTask != null && (FLastTask.Status == TaskStatus.Running || FLastTask.Status == TaskStatus.WaitingForActivation));

            return FStreamProvider;
        }
    }

    /// <summary>
    /// Asynchronously writes bytes to a stream
    /// </summary>
    [ProcessNode]
    public class WriterBytes : Writer<Spread<byte>>
    {
        public void Update(IResourceProvider<Stream> input, Spread<byte> data, long offset, bool write, bool abort, out float progress, out bool inProgress)
        {
            Write(input, data, offset, write, abort, out progress, out inProgress, WriteAsync);
        }

        private async Task WriteAsync(IResourceProvider<Stream> provider, Spread<byte> data, long offset, IProgress<float> progress, CancellationToken ct)
        {
            using (var handle = provider.GetHandle())
            {
                var stream = handle.Resource;
                if (stream.CanSeek)
                    stream.Position += offset;

                var buffer = StreamUtils.LargeBuffer;
                var bytesToWrite = data.Count;
                var bytesWritten = 0;
                var st = Stopwatch.StartNew();
                while (bytesWritten < bytesToWrite && !ct.IsCancellationRequested)
                {
                    var chunkSize = Math.Min(bytesToWrite - bytesWritten, buffer.Length);
                    data.CopyTo(bytesWritten, buffer, 0, chunkSize);
                    await stream.WriteAsync(buffer, 0, chunkSize);
                    bytesWritten += chunkSize;

                    // Report every 40ms (25 fps)
                    if (st.ElapsedMilliseconds >= 40)
                    {
                        progress.Report(bytesWritten / (float)bytesToWrite);
                        st.Restart();
                    }
                }

                // Report the final value
                progress.Report(1f);
            }
        }
    }

    /// <summary>
    /// Asynchronously writes strings to a stream
    /// </summary>
    [ProcessNode]
    public class WriterString : Writer<string>
    {
        public void Update(IResourceProvider<Stream> input, Encodings encoding, string data, long offset, bool write, bool abort, out float progress, out bool inProgress)
        {
            Write(input, data, offset, write, abort, out progress, out inProgress, (a,b,c,d,e) => WriteAsync(a,b,c,d,e,encoding));
        }

        private async Task WriteAsync(IResourceProvider<Stream> provider, string data, long offset, IProgress<float> progress, CancellationToken ct, Encodings encoding)
        {
            using (var handle = provider.GetHandle())
            {
                var stream = handle.Resource;

                var dataArray = encoding.ToEncoding().GetBytes(data);

                if (stream.CanSeek)
                {
                    stream.Position += offset;
                }

                int bytesToWrite = StreamUtils.SmallBufferSize;
                long bytesWritten = 0;

                while (bytesWritten < dataArray.Length && !ct.IsCancellationRequested)
                {
                    bytesToWrite = Math.Min(bytesToWrite, (int)(dataArray.Length - bytesWritten));
                    await stream.WriteAsync(dataArray, (int)bytesWritten, bytesToWrite);
                    bytesWritten += bytesToWrite;
                    progress.Report(bytesWritten / (float)dataArray.Length);
                }
                ct.ThrowIfCancellationRequested();
            }
        }
    }
}

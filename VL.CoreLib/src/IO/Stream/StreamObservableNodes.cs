using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;
using VL.Core;

namespace VL.Lib.IO
{
    public class ReadBytesObservable : IDisposable
    {
        private IResourceProvider<Stream> FStreamProvider;
        private int FChunkSize;
        private long FOffset;
        private long FCount;

        private IObservable<Spread<byte>> FData;
        private Task FLastTask;
        private CancellationTokenSource FLastCancellationTokenSource;
        private float FProgress;

        public ReadBytesObservable()
        {
            FChunkSize = -10;
            FOffset = -10;
            FCount = -10;

            FData = Observable.Empty<Spread<byte>>();
            FProgress = 0.0f;
        }

        public void Dispose()
        {
            if (FLastCancellationTokenSource != null)
                FLastCancellationTokenSource.Cancel();
        }

        public IResourceProvider<Stream> ReadAll(IResourceProvider<Stream> streamProvider, long offset, out float progress, out bool inProgress, out IObservable<Spread<byte>> data, int chunkSize = 4096, bool read = false, bool abort = false)
        {
            return Read(streamProvider, offset, long.MaxValue, out progress, out inProgress, out data, chunkSize, read, abort);
        }

        public IResourceProvider<Stream> Read(IResourceProvider<Stream> streamProvider, long offset, long count, out float progress, out bool inProgress, out IObservable<Spread<byte>> data, int chunkSize = 4096,  bool read = false, bool abort = false)
        {
            if (FStreamProvider != streamProvider) //input resource provider changed - abort
            {
                FStreamProvider = streamProvider;
                abort = true;
            }

            abort |= (FChunkSize != chunkSize || FOffset != offset || FCount != count || read); //other input params changed - abort

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

            if (read) //fire task if not yet running
            {
                FChunkSize = chunkSize;
                FOffset = offset;
                FCount = count;

                var cts = new CancellationTokenSource();
                FLastCancellationTokenSource = cts;
                var p = new Progress<float>(x => { if (!cts.IsCancellationRequested) { FProgress = x; } });

                FData = Observable.Create<Spread<byte>>(o => FLastTask = Task.Run(()=>ReadChunk(o, streamProvider, offset, count, chunkSize, p, cts.Token))
                            ).Publish().RefCount();
                
            }
            progress = FProgress;
            data = FData;
            inProgress = (FLastTask != null && (FLastTask.Status == TaskStatus.Running || FLastTask.Status == TaskStatus.WaitingForActivation));
            if (!inProgress)
                progress = 0;

            return FStreamProvider;
        }

        async Task ReadChunk(IObserver<Spread<byte>> observer, IResourceProvider<Stream> provider, long offset, long count, int chunkSize, IProgress<float> progress, CancellationToken ct)
        {
            using (var handle = provider.GetHandle())
            {
                if (count != 0)
                { 
                    var stream = handle.Resource;
                    if (stream.CanSeek)
                    {
                        stream.Position += offset;

                        if (count == long.MaxValue)
                            count = stream.Length;
                    }

                    byte[] buffer = new byte[chunkSize];
                    long bytesRead = 0;
                    while (bytesRead < count && !ct.IsCancellationRequested)
                    {
                        chunkSize = Math.Min(chunkSize, (int)(count - bytesRead));
                        var read = await stream.ReadAsync(buffer, 0, chunkSize);

                        bytesRead += read;
                        progress.Report(bytesRead / (float)count);

                        observer.OnNext(buffer.ToSpread().GetSpread(0, read));
                    }
                    ct.ThrowIfCancellationRequested();
                }   
                observer.OnCompleted();
            }
        }
    }
}

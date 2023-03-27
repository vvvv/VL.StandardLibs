#nullable enable
using Microsoft.FSharp.Core;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;
using VL.Lib.Basics.Resources;
using NetSocket = System.Net.Sockets.Socket;

namespace VL.Lib.IO.Socket
{
    /// <summary>
    /// Sends datagrams on a local socket.
    /// </summary>
    public class DatagramSender : IDisposable
    {
        private readonly SerialDisposable FSubscription = new SerialDisposable();

        object? FLocalSocketProvider;
        object? FDatagrams;

        public DatagramSender(NodeContext nodeContext)
        {
        }

        /// <summary>
        /// Configures the sender.
        /// </summary>
        /// <param name="localSocket">The local socket to send data out of.</param>
        /// <param name="datagrams">The datagrams to send.</param>
        public void Update(IResourceProvider<NetSocket> localSocket, IObservable<Datagram> datagrams)
        {
            if (localSocket != FLocalSocketProvider || datagrams != FDatagrams)
            {
                FLocalSocketProvider = localSocket;
                FDatagrams = datagrams;
                if (localSocket != null)
                    FSubscription.Disposable = Subscribe(localSocket, datagrams);
                else
                    FSubscription.Disposable = null;
            }
        }

        IDisposable Subscribe(IResourceProvider<NetSocket> provider, IObservable<Datagram> datagrams)
        {
            return Observable.Using(
                () => new AsyncSocketHelper(provider),
                x =>
                {
                    var args = x.Args;
                    var awaitable = x.Awaitable;
                    var socket = x.Socket;
                    if (socket is null)
                        return Observable.Empty<Unit>();

                    return datagrams
                        .ObserveOn(Scheduler.Default)
                        .SelectMany(async datagram =>
                        {
                            try
                            {
                                var sentBytes = 0;
                                var data = datagram.PayloadArray;
                                while (sentBytes < data.Length)
                                {
                                    var bytesToSend = Math.Min(args.Buffer!.Length, data.Length - sentBytes);
                                    Buffer.BlockCopy(data, sentBytes, args.Buffer, 0, bytesToSend);
                                    args.SetBuffer(0, bytesToSend);
                                    args.RemoteEndPoint = datagram.RemoteEndPoint;
                                    await socket.SendToAsync(awaitable);
                                    if (args.BytesTransferred > 0)
                                        sentBytes += args.BytesTransferred;
                                    else
                                    {
                                        Trace.TraceError("Failed to send datagram");
                                        break;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                RuntimeGraph.ReportException(e);
                            }

                            return default(Unit);
                        });
                })
                .SubscribeOn(Scheduler.Default)
                .Subscribe();
        }

        void IDisposable.Dispose()
        {
            FSubscription.Dispose();
        }

        sealed class AsyncSocketHelper : IDisposable
        {
            private readonly CompositeDisposable disposables = new CompositeDisposable();

            public AsyncSocketHelper(IResourceProvider<NetSocket> provider)
            {
                var handle = provider.GetHandle().DisposeBy(disposables);
                Socket = handle.Resource;
                Args = new SocketAsyncEventArgs().DisposeBy(disposables);
                Args.SetBuffer(new byte[0x20000], 0, 0x20000);
                Awaitable = new SocketAwaitable(Args);
            }

            public NetSocket Socket { get; }

            public SocketAsyncEventArgs Args { get; }

            public SocketAwaitable Awaitable { get; }

            public void Dispose()
            {
                disposables.Dispose();
            }
        }
    }
}
#nullable restore
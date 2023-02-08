using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.Threading;
using NetSocket = System.Net.Sockets.Socket;

namespace VL.Lib.IO.Socket
{
    /// <summary>
    /// Sends datagrams on a local socket.
    /// </summary>
    public class DatagramSender : IDisposable
    {
        CancellationTokenSource FCancellation = new CancellationTokenSource();
        object FLocalSocketProvider;
        object FDatagrams;
        Task FCurrentTask;

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
                Stop(0);
                if (localSocket != null)
                    Start(localSocket, datagrams);
            }
        }

        void Start(IResourceProvider<NetSocket> provider, IObservable<Datagram> datagrams)
        {
            FCancellation = new CancellationTokenSource();
            var token = FCancellation.Token;
            FCurrentTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    using (var handle = await provider.GetHandleAsync(token, 100))
                    using (var args = new SocketAsyncEventArgs())
                    {
                        var socket = handle.Resource;
                        if (socket == null)
                            return;

                        args.SetBuffer(new byte[0x20000], 0, 0x20000);
                        var awaitable = new SocketAwaitable(args);

                        // Return the handle on cancellation
                        token.Register(handle.Dispose);

                        foreach (var datagram in datagrams.ToEnumerable())
                        {
                            if (token.IsCancellationRequested)
                                break;

                            try
                            {
                                var sentBytes = 0;
                                var data = datagram.PayloadArray;
                                while (sentBytes < data.Length)
                                {
                                    var bytesToSend = Math.Min(args.Buffer.Length, data.Length - sentBytes);
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
                                break;
                            }
                        }
                    }
                }
            }, token);
        }

        private void Stop(int timeout)
        {
            FCurrentTask?.CancelAndDispose(FCancellation, timeout);
            FCurrentTask = null;
        }

        void IDisposable.Dispose()
        {
            Stop(1);
        }
    }
}

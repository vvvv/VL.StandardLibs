using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;
using VL.Lib.Threading;
using NetSocket = System.Net.Sockets.Socket;
using NetUtils = VL.Lib.IO.Net.NetUtils;

namespace VL.Lib.IO.Socket
{
    /// <summary>
    /// Receives datagrams from a local socket.
    /// </summary>
    public class DatagramReceiver : IDisposable
    {
        readonly Subject<Datagram> FOutput = new Subject<Datagram>();
        CancellationTokenSource FCancellation = new CancellationTokenSource();
        IResourceProvider<NetSocket> FLocalSocketProvider;
        Task FCurrentTask;

        /// <summary>
        /// The observable sequence of datagrams. The datagrams will be pushed on the network thread.
        /// </summary>
        public IObservable<Datagram> Datagrams => FOutput;

        /// <summary>
        /// Configures the receiver.
        /// </summary>
        /// <param name="localSocket">The local socket to receive data from.</param>
        public void Update(IResourceProvider<NetSocket> localSocket)
        {
            if (localSocket != FLocalSocketProvider)
            {
                FLocalSocketProvider = localSocket;
                Stop(0);
                if (localSocket != null)
                    Start(localSocket);
            }
        }

        void Start(IResourceProvider<NetSocket> provider)
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

                        args.SetBuffer(new byte[0x10000], 0, 0x10000);
                        var awaitable = new SocketAwaitable(args);

                        // Return the handle on cancellation
                        token.Register(handle.Dispose);

                        // Do processing, continually receiving from the socket 
                        try
                        {
                            while (!token.IsCancellationRequested)
                            {
                                args.RemoteEndPoint = NetUtils.DefaultIPEndPoint;
                                await socket.ReceiveFromAsync(awaitable);
                                if (!token.IsCancellationRequested)
                                {
                                    var bytesRead = args.BytesTransferred;
                                    if (bytesRead > 0)
                                    {
                                        var data = new byte[bytesRead];
                                        Buffer.BlockCopy(args.Buffer, 0, data, 0, bytesRead);
                                        var datagram = new Datagram((IPEndPoint)args.RemoteEndPoint, data);
                                        FOutput.OnNext(datagram);
                                    }
                                    else
                                    {
                                        // Connection lost
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // Try again
                            await Task.Delay(100);
                            continue;
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

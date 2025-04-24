using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;
using VL.Lib.IO.Socket;
using VL.Lib.Threading;
using NetSocket = System.Net.Sockets.Socket;
using NetUtils = VL.Lib.IO.Net.NetUtils;

[assembly: ImportType(typeof(DatagramReceiver), Name = "Receiver (Datagram)", Category = "IO.Socket.Advanced")]

namespace VL.Lib.IO.Socket
{
    /// <summary>
    /// Receives datagrams from a local socket.
    /// </summary>
    [ProcessNode]
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
                var buffer = new byte[0x10000];
                while (!token.IsCancellationRequested)
                {
                    using (var handle = await provider.GetHandleAsync(token, 100))
                    {
                        var socket = handle.Resource;
                        if (socket == null)
                            return;

                        // Return the handle on cancellation
                        token.Register(handle.Dispose);

                        // Do processing, continually receiving from the socket 
                        try
                        {
                            while (!token.IsCancellationRequested)
                            {
                                var result = await socket.ReceiveFromAsync(buffer, NetUtils.DefaultIPEndPoint, token);
                                var bytesRead = result.ReceivedBytes;
                                if (bytesRead > 0)
                                {
                                    var data = new byte[bytesRead];
                                    Buffer.BlockCopy(buffer, 0, data, 0, bytesRead);

                                    var datagram = new Datagram((IPEndPoint)result.RemoteEndPoint, data);
                                    FOutput.OnNext(datagram);
                                }
                                else
                                {
                                    // Connection lost
                                    break;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            if (!token.IsCancellationRequested)
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

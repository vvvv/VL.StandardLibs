#nullable enable
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Basics.Resources;
using VL.Lib.IO.Socket;
using VL.Lib.Reactive;
using NetSocket = System.Net.Sockets.Socket;

[assembly: ImportType(typeof(DatagramSender), Name = "Sender (Datagram)", Category = "IO.Socket.Advanced")]

namespace VL.Lib.IO.Socket
{
    /// <summary>
    /// Sends datagrams on a local socket.
    /// </summary>
    [ProcessNode]
    public class DatagramSender : IDisposable
    {
        private readonly SerialDisposable FSubscription = new SerialDisposable();
        private readonly NodeContext FNodeContext;
        private readonly ILogger FLogger;

        object? FLocalSocketProvider;
        object? FDatagrams;

        public DatagramSender(NodeContext nodeContext)
        {
            FNodeContext = nodeContext;
            FLogger = nodeContext.GetLogger();
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
                () => provider.GetHandle(),
                x =>
                {
                    var socket = x.Resource;
                    if (socket is null)
                        return Observable.Empty<Unit>();

                    const int bufferLength = 0x20000;

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
                                    var bytesToSend = Math.Min(bufferLength, data.Length - sentBytes);
                                    var buffer = new ArraySegment<byte>(datagram.PayloadArray, sentBytes, bytesToSend);
                                    var bytesTransferred = await socket.SendToAsync(buffer, datagram.RemoteEndPoint);
                                    if (bytesTransferred > 0)
                                        sentBytes += bytesTransferred;
                                    else
                                    {
                                        FLogger.LogError("Failed to send datagram");
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
                .CatchAndReport(FNodeContext)
                .SubscribeOn(Scheduler.Default)
                .Subscribe();
        }

        void IDisposable.Dispose()
        {
            FSubscription.Dispose();
        }
    }
}
#nullable restore
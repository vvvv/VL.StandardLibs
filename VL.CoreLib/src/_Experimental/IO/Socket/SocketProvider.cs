using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NetSocket = System.Net.Sockets.Socket;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;
using VL.Lib.Animation;
using System.Threading;
using System.Threading.Tasks;
using VL.Lib.IO.Net;
using VL.Core.Import;
using VL.Lib.IO.Socket;

[assembly: ImportType(typeof(SocketProvider), Category = "Experimental.IO.Socket")]

namespace VL.Lib.IO.Socket
{
    /// <summary>
    /// Returns a very basic socket type
    /// </summary>
    [ProcessNode]
    public class SocketProvider
    {
        IFrameClock FClock;
        IResourceProvider<NetSocket> FSocket;
        ResourceProviderMonitor<NetSocket> Monitor;
        SocketType FSocketType;
        ProtocolType FProtocolType;
        IPAddress FLocalIP;
        int FLocalPort;

        public bool IsAlive => Monitor?.SinkCount > 0;
        public IResourceProvider<NetSocket> Current => FSocket;
            
        public SocketProvider(IFrameClock clock)
        {
            FClock = clock;
            FSocket = null;
        }

        public IResourceProvider<NetSocket> Update(SocketType socketType, ProtocolType protocolType, IPAddress localAddress, int localPort, out bool changed)
        {
            changed = false;
            FSocket = CreateSocketProvider(socketType, protocolType, localAddress, localPort, out changed);
            return FSocket;
        }

        internal IResourceProvider<NetSocket> CreateSocketProvider(SocketType socketType, ProtocolType protocolType, IPAddress localAddress, int localPort, out bool changed, Action<NetSocket> applyBeforeSharing = null, int skipDisposal = 1, bool forceUpdate = false)
        {
            changed = false;
            if (FSocket == null || forceUpdate ||
                FSocketType != socketType || FProtocolType != protocolType ||
                FLocalIP != localAddress || FLocalPort != localPort)
            {
                FSocketType = socketType;
                FProtocolType = protocolType;
                FLocalIP = localAddress;
                FLocalPort = localPort;

                var source = ResourceProvider.New(() => CreateAndBind(socketType, protocolType, localAddress, localPort));
                Monitor = source.Monitor();

                if (applyBeforeSharing == null)
                    applyBeforeSharing = (s) => { };

                var x = FClock.GetTicks();
                if (skipDisposal > 0)
                    x = x.Skip(skipDisposal);

                FSocket = Monitor.Do(applyBeforeSharing).ShareSerially(x, null);
                changed = true;
            }
            return FSocket;
        }

        public NetSocket CreateAndBind(SocketType socketType, ProtocolType protocolType, IPAddress ip, int port)
        {
            var socket = new NetSocket(socketType, protocolType);
            socket.ExclusiveAddressUse = false;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(ip, port));
            return socket;
        }
    }

    /// <summary>
    /// Returns socket configured as TCP Client
    /// </summary>
    public class TCPClientSocket : IDisposable
    {
        SocketProvider FProvider;
        IPAddress FRemoteIP;
        int FRemotePort;
        bool FClosed;

        CancellationTokenSource FCTS;
        Task FConnectionCheckTask;

        public bool Connected { get; private set; }

        public TCPClientSocket(IFrameClock clock)
        {
            FProvider = new SocketProvider(clock);
            FConnectionCheckTask = new Task(() =>
            {
                FCTS?.Cancel();
                Connected = false;
                FClosed = false;
                FCTS = new CancellationTokenSource();
            });
        }

        public void Dispose()
        {
            FCTS?.Cancel();
        }

        public IResourceProvider<NetSocket> Update(IPAddress localAddress, int localPort, IPAddress remoteAddress, out bool changed, int remotePort = 4444)
        {
            changed = false;
            if (FRemoteIP != remoteAddress || FRemotePort != remotePort || (Connected && FClosed)) //in case remote changed, we have to dispose an existing socket
            {
                FRemoteIP = remoteAddress;
                FRemotePort = remotePort;
                
                FCTS?.Cancel();
                Connected = false;
                FClosed = false;
                changed = true;
            }
            
            FProvider.CreateSocketProvider(SocketType.Stream, ProtocolType.Tcp, localAddress, localPort, out changed, s => 
                {
                    FConnectionCheckTask = new Task(() => CheckConnectionStatus(s, remoteAddress, remotePort));
                    FConnectionCheckTask.Start(TaskScheduler.Default);
                },  1, changed); 
            return FProvider.Current;
        }

        void CheckConnectionStatus(NetSocket socket, IPAddress remoteAddress, int remotePort)
        {
            FCTS?.Cancel();
            Connected = false;
            FClosed = false;
            FCTS = new CancellationTokenSource();
            CheckConnectionStatusLoop(socket, remoteAddress, remotePort, FCTS.Token).ToList();
        }

        IEnumerable<bool> CheckConnectionStatusLoop(NetSocket socket, IPAddress remoteAddress, int remotePort, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!Connected)
                {
                    bool connected = CheckConnectionEstablished(socket, remoteAddress, remotePort);
                    yield return connected;
                    if (!token.IsCancellationRequested)
                        Connected = connected;
                    Thread.Sleep(250);
                }
                else
                {
                    bool closed = SocketUtils.CheckIfClosed(socket);
                    yield return !closed;
                    if (!token.IsCancellationRequested)
                        FClosed = closed;
                    Thread.Sleep(500);
                }
            }
        }

        bool CheckConnectionEstablished(NetSocket socket, IPAddress remoteAddress, int remotePort)
        {
            try
            {
                socket.Blocking = false;
                socket.Connect(new IPEndPoint(remoteAddress, remotePort));
                socket.Blocking = true;
                return true;
            }
            catch (Exception e)
            {
                if ((e as SocketException)?.ErrorCode == 10056) //already connected. no idea how we could get here, but we do
                {
                    socket.Blocking = true;
                    return true;
                }
                else 
                {
                    try //socket might be disposed already when coming here
                    {
                        socket.Blocking = true;
                    }
                    catch { }
                    return false;
                }
            }
        }
    }

    /// <summary>
    /// Creates a TCP server socket
    /// </summary>
    public class TCPServerSocket : IDisposable
    {
        IFrameClock FClock;
        SocketProvider FProvider;

        ConcurrentDictionary<IPEndPoint, ConcurrentQueue<Spread<byte>>> FInputQueue; //packets to be sent
        BehaviorSubject<IObservable<IResourceProvider<NetSocket>>> FConnectedSockets; //emits child sockets (incoming connections)
        IEnumerator<IList<Tuple<Spread<byte>, IPEndPoint>>> FEnumerator; //subscribes on the sockets and enumerates their output data

        public Spread<IPEndPoint> RemoteHosts => FInputQueue.Keys.ToSpread();

        public IObservable<Tuple<Spread<byte>, IPEndPoint>> Data { get; private set; }

        public TCPServerSocket(IFrameClock clock)
        {
            FClock = clock;
            FProvider = new SocketProvider(clock);
            FInputQueue = new ConcurrentDictionary<IPEndPoint, ConcurrentQueue<Spread<byte>>>();

            FConnectedSockets = new BehaviorSubject<IObservable<IResourceProvider<NetSocket>>>(Observable.Empty<IResourceProvider<NetSocket>>());
            //FConnectedSockets.Switch().Publish().RefCount();
        }

        public void Dispose()
        {
            FEnumerator?.Dispose();
        }

        public IResourceProvider<NetSocket> Update(IPAddress localAddress, 
                                                    int localPort,
                                                    out Spread<Tuple<Spread<byte>, IPEndPoint>> dataOutput,
                                                    out bool changed, 
                                                    IEnumerable<Tuple<Spread<byte>, IPEndPoint>> data, 
                                                    Func<IResourceProvider<NetSocket>, IEnumerable<IEnumerable<byte>>, Spread<byte>> region, 
                                                    bool restart)
        {
            changed = false;

            foreach (var d in data)
                EnqueueIncomingData(d);

            var socketProvider = FProvider.CreateSocketProvider(SocketType.Stream, ProtocolType.Tcp, localAddress, localPort, out changed);
            if (changed)
            {
                using (var h = socketProvider.GetHandle())
                    h.Resource.Listen(100);

                //start listen accept client sockets;
                FConnectedSockets.OnNext(AcceptLooped(socketProvider).ToObservable(ThreadPoolScheduler.Instance));
                ApplyRegion(region);
            }

            if (restart) //HACK probably will be needed while patching since due to missing state restore
                ApplyRegion(region);

            if (FEnumerator != null && FEnumerator.MoveNext())
                dataOutput = FEnumerator.Current.ToSpread();
            else
                dataOutput = Spread<Tuple<Spread<byte>,IPEndPoint>>.Empty; 
                
            return socketProvider;
        }

        void ApplyRegion(Func<IResourceProvider<NetSocket>, IEnumerable<IEnumerable<byte>>, Spread<byte>> region)
        {
            FEnumerator?.Dispose();
            FEnumerator = FConnectedSockets
                    .SelectMany(o => o)
                    .Select(s => Loop(s, region).ToObservable(ThreadPoolScheduler.Instance))
                    .SelectMany(t => t)
                    .Chunkify()
                    .GetEnumerator();
        }

        IEnumerable<Tuple<Spread<byte>, IPEndPoint>> Loop(IResourceProvider<NetSocket> provider, Func<IResourceProvider<NetSocket>, IEnumerable<IEnumerable<byte>>, Spread<byte>> region) 
        {
            try
            {
                while (true)
                {
                    using (var s = provider.GetHandle())
                        if (SocketUtils.CheckIfClosed(s.Resource))
                            break;

                    var ep = NetUtils.DefaultIPEndPoint;
                    using (var h = provider.GetHandle())
                        ep = (IPEndPoint)h.Resource.RemoteEndPoint;
                    yield return Tuple.Create(region(provider, GetAssignedData(ep)), ep);
                }
            }
            finally { }
        }

        IEnumerable<IEnumerable<byte>> GetAssignedData(IPEndPoint ep)
        {
            Spread<byte> packet = Spread<byte>.Empty;
            ConcurrentQueue<Spread<byte>> b;
            if (FInputQueue.TryGetValue(ep, out b))
                while (b.TryDequeue(out packet))
                    yield return packet;
        }

        void EnqueueIncomingData(Tuple<Spread<byte>, IPEndPoint> o)
        {
            ConcurrentQueue<Spread<byte>> q;
            var ipep = new IPEndPoint(o.Item2.Address.MapToIPv6(), o.Item2.Port);
            if (FInputQueue.TryGetValue(ipep, out q)) //write message to according data queue
            {
                q.Enqueue(o.Item1);
                FInputQueue.AddOrUpdate(ipep, q, (k, oldval) => q);
            }
            if (o.Item2.Address == IPAddress.Any || o.Item2.Port == 0 || o.Item2.Port == 255)
            {
                foreach (var queues in FInputQueue.Values)
                    queues.Enqueue(o.Item1);
            }
        }

        IEnumerable<IResourceProvider<NetSocket>> AcceptLooped(IResourceProvider<NetSocket> socket)
        {
            while (true)
            {
                using (var h = socket.GetHandle())
                {
                    foreach (var s in AcceptSockets(h.Resource))
                    {
                        var ep = (IPEndPoint)s.RemoteEndPoint;
                        FInputQueue.TryAdd(ep, new ConcurrentQueue<Spread<byte>>());
                        yield return ResourceProvider.New(
                            () => s,
                            (i) => 
                            {
                                var endpoint = (IPEndPoint)i.RemoteEndPoint;
                                ConcurrentQueue<Spread<byte>> q;
                                while (!FInputQueue.TryRemove(endpoint, out q)) { }
                            }
                            ).ShareSerially(FClock.GetTicks().Skip(2).Select(o => Observable.Timer(TimeSpan.FromMilliseconds(160))),null);

                        /*.Delay(TimeSpan.FromMilliseconds(160) // Subscription somehow super expensive?!*/
                    }
                }
            }
        }

        IEnumerable<NetSocket> AcceptSockets(NetSocket socket)
        {
            while (socket.Poll(60000, SelectMode.SelectRead))
            {
                yield return socket.Accept();
                Thread.Sleep(100);
            }
        }
    }
}

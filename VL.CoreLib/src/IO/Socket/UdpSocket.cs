using System.Net;
using System.Net.Sockets;
using VL.Core.Import;
using VL.Lib.Basics.Resources;
using VL.Lib.IO.Socket;
using NetSocket = System.Net.Sockets.Socket;
using NetUtils = VL.Lib.IO.Net.NetUtils;

[assembly: ImportType(typeof(UdpSocket), Category = "IO.Socket.Advanced")]

namespace VL.Lib.IO.Socket
{
    /// <summary>
    /// Manages a UDP socket provider.
    /// </summary>
    [ProcessNode]
    public class UdpSocket
    {
        ResourceProviderMonitor<NetSocket> FCurrentProvider;
        IPEndPoint FLocalIPEndPoint;
        bool FBind;

        /// <summary>
        /// Configures the internally managed socket provider.
        /// </summary>
        /// <param name="localEndPoint">The local end point to use.</param>
        /// <param name="bind">Whether or not to bind the socket.</param>
        /// <param name="enabled">Whether or not the socket is active.</param>
        /// <returns>A socket provider which can be used by multiple threads in parallel.</returns>
        public IResourceProvider<NetSocket> Update(IPEndPoint localEndPoint, bool bind, bool enabled = true)
        {
            if (!NetUtils.Equals(localEndPoint, FLocalIPEndPoint) || bind != FBind)
            {
                FLocalIPEndPoint = localEndPoint;
                FBind = bind;
                FCurrentProvider = ResourceProvider.New(() =>
                {
                    var socket = new NetSocket(SocketType.Dgram, ProtocolType.Udp);
                    socket.ExclusiveAddressUse = false;
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    if (bind && localEndPoint != null)
                        socket.Bind(localEndPoint);
                    return socket;
                }).ShareInParallel().Monitor();
            }
            if (enabled)
                return FCurrentProvider;
            return null;
        }

        /// <summary>
        /// Whether or not the socket is open.
        /// </summary>
        public bool IsOpen => FCurrentProvider?.SinkCount > 0;
    }
}

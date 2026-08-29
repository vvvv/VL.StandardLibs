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
    /// Manages a UDP socket client provider.
    /// </summary>
    [ProcessNode]
    public class UdpSocketClient
    {
        ResourceProviderMonitor<NetSocket> FCurrentProvider;
        private bool FConnect = false;
        private IPEndPoint FRemoteIPEndPoint;

        /// <summary>
        /// Configures the internally managed socket provider.
        /// </summary>
        /// <param name="remoteEndpoint">The remote end point to use.</param>
        /// <param name="connect">Whether or not to connect the socket.</param>
        /// <param name="enabled">Whether or not the socket is active.</param>
        /// <returns>A socket provider which can be used by multiple threads in parallel.</returns>
        public IResourceProvider<NetSocket> Update(IPEndPoint remoteEndpoint, bool connect, bool enabled = true)
        {
            if (!NetUtils.Equals(FRemoteIPEndPoint, remoteEndpoint) || FConnect != connect)
            {
                FConnect = connect;
                FRemoteIPEndPoint = remoteEndpoint;

                FCurrentProvider = ResourceProvider.New(() =>
                {
                    var socket = new NetSocket(SocketType.Dgram, ProtocolType.Udp);
                    socket.ExclusiveAddressUse = false;
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    if (connect && remoteEndpoint != null)
                        socket.Connect(remoteEndpoint);
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

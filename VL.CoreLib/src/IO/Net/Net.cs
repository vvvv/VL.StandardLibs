using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;
using VL.Lib.Collections;

namespace VL.Lib.IO.Net
{
    /// <summary>
    /// A couple of utility functions around IP addresses and end points.
    /// </summary>
    public static class NetUtils
    {
        private const string LOCALHOST = "localhost";

        /// <summary>
        /// The default IP address to use for unconnected pins. Points to the 127.0.0.1
        /// </summary>
        public static readonly IPAddress DefaultAddress = IPAddress.Loopback;

        /// <summary>
        /// The default IP end point to use for unconnected pins. Points to 127.0.0.1 and port 0
        /// </summary>
        public static readonly IPEndPoint DefaultIPEndPoint = new IPEndPoint(DefaultAddress, 0);

        /// <summary>
        /// An IP end point which points to nowhere.
        /// </summary>
        public static readonly IPEndPoint NoIPEndPoint = new IPEndPoint(IPAddress.None, 0);

        /// <summary>
        /// Returns the Internet Protocol (IP) addresses for the specified host
        /// </summary>
        /// <param name="hostNameOrAddress"></param>
        /// <returns></returns>
        public static Spread<IPAddress> GetHostAddresses(string hostNameOrAddress = LOCALHOST)
            => Dns.GetHostAddresses(hostNameOrAddress).ToSpread();

        /// <summary>
        /// Returns the Internet Protocol (IP) addresses for the specified host asynchronously
        /// </summary>
        /// <param name="hostNameOrAddress"></param>
        /// <returns></returns>
        public static IObservable<Spread<IPAddress>> GetHostAddressesAsync(string hostNameOrAddress = LOCALHOST)
            => Observable.StartAsync(() => Dns.GetHostAddressesAsync(hostNameOrAddress)).Select(a => a.ToSpread());

        /// <summary>
        /// Returns the IP end point for the specified host and port
        /// </summary>
        /// <param name="hostNameOrAddress"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static IPEndPoint GetIPEndPoint(string hostNameOrAddress, int port)
        {
            IPAddress ipAddress;
            if (string.IsNullOrWhiteSpace(hostNameOrAddress))
                return new IPEndPoint(IPAddress.Any, port);
            if (IPAddress.TryParse(hostNameOrAddress, out ipAddress))
                return new IPEndPoint(ipAddress, port);
            var ipAddresses = Dns.GetHostAddresses(hostNameOrAddress);
            ipAddress = ipAddresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
            if (ipAddress != null)
                return new IPEndPoint(ipAddress, port);
            throw new Exception($"Couldn't resolve {hostNameOrAddress} to an IP version 4 address.");
        }

        /// <summary>
        /// Returns the IP end point for the specified host and port asynchronously
        /// </summary>
        /// <param name="hostNameOrAddress"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static IObservable<IPEndPoint> GetIPEndPointAsync(string hostNameOrAddress, int port)
            => Observable.StartAsync(() => _GetIPEndPointAsync(hostNameOrAddress, port));

        static async Task<IPEndPoint> _GetIPEndPointAsync(string hostNameOrAddress, int port)
        {
            IPAddress ipAddress;
            if (string.IsNullOrWhiteSpace(hostNameOrAddress))
                return new IPEndPoint(IPAddress.Any, port);
            if (IPAddress.TryParse(hostNameOrAddress, out ipAddress))
                return new IPEndPoint(ipAddress, port);
            var ipAddresses = await Dns.GetHostAddressesAsync(hostNameOrAddress);
            ipAddress = ipAddresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
            if (ipAddress != null)
                return new IPEndPoint(ipAddress, port);
            throw new Exception($"Couldn't resolve {hostNameOrAddress} to an IP version 4 address.");
        }
    }
}

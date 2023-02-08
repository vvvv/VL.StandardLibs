using System.Net;
using VL.Lib.Collections;
using VL.Lib.IO.Net;

namespace VL.Lib.IO.Socket
{
    /// <summary>
    /// A datagram is a little message used in connection less network protocols (like UDP). 
    /// </summary>
    public struct Datagram
    {
        readonly IPEndPoint FRemoteEndPoint;
        readonly byte[] FPayload;

        /// <summary>
        /// Creates a datagram with a mutable array of bytes as data.
        /// </summary>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="payload">The payload.</param>
        public Datagram(IPEndPoint remoteEndPoint, byte[] payload)
        {
            FRemoteEndPoint = remoteEndPoint;
            FPayload = payload;
        }

        /// <summary>
        /// Creates a datagram with a spread of bytes as data.
        /// </summary>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="payload">The payload.</param>
        public Datagram(IPEndPoint remoteEndPoint, Spread<byte> payload)
            : this(remoteEndPoint, payload.GetInternalArray())
        {
        }

        /// <summary>
        /// Splits the datagram into its remote end point and payload.
        /// </summary>
        /// <param name="remoteEndPoint">The remote end point from which this datagram was received or will be sent to. Returns none if it's the default value.</param>
        /// <param name="payload">The payload of the datagram. Returns the empty spread if it's the default value.</param>
        public void Split(out IPEndPoint remoteEndPoint, out Spread<byte> payload)
        {
            remoteEndPoint = RemoteEndPoint;
            payload = Payload;
        }

        /// <summary>
        /// Splits the datagram into its remote end point and payload as raw bytes.
        /// </summary>
        /// <param name="remoteEndPoint">The remote end point from which this datagram was received or will be sent to. Returns none if it's the default value.</param>
        /// <param name="payload">The payload of the datagram. Returns the empty array if it's the default value.</param>
        public void SplitArray(out IPEndPoint remoteEndPoint, out byte[] payload)
        {
            remoteEndPoint = RemoteEndPoint;
            payload = PayloadArray;
        }

        /// <summary>
        /// The remote end point this datagram shall be sent to or was received from.
        /// </summary>
        public IPEndPoint RemoteEndPoint => FRemoteEndPoint ?? NetUtils.NoIPEndPoint;

        /// <summary>
        /// The content of this datagram as spread.
        /// </summary>
        public Spread<byte> Payload => FPayload?.AsSpreadUnsafe() ?? Spread<byte>.Empty;

        /// <summary>
        /// The content of this datagram as raw bytes.
        /// </summary>
        public byte[] PayloadArray => FPayload ?? new byte[0];
    }
}

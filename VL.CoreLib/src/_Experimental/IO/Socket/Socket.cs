using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NetSocket = System.Net.Sockets.Socket;
using System.Reactive.Linq;
using VL.Lib.Collections;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.IO.Net;

namespace VL.Lib.IO.Socket
{
    public static class SocketUtils
    {
        public static IResourceProvider<NetSocket> LocalEndPoint(this IResourceProvider<NetSocket> input, out IPEndPoint endpoint)
        {
            endpoint = NetUtils.DefaultIPEndPoint;
            if (input != null)
                using (var h = input.GetHandle())
                    endpoint = (IPEndPoint)h.Resource.LocalEndPoint;
            return input;
        }

        public static IResourceProvider<NetSocket> RemoteEndPoint(this IResourceProvider<NetSocket> input, out IPEndPoint endpoint)
        {
            endpoint = NetUtils.DefaultIPEndPoint;
            if (input != null)
                using (var h = input.GetHandle())
                    endpoint = (IPEndPoint)h.Resource.RemoteEndPoint;
            return input;
        }

        public static IResourceProvider<NetSocket> Send(IResourceProvider<NetSocket> input, int timeOut, IEnumerable<byte> data, bool send, out bool timedOut)
        {
            timedOut = false;
            if (send && input != null)
            {
                using (var h = input.GetHandle())
                {
                    var socket = h.Resource;
                    try
                    {
                        socket.SendTimeout = timeOut;
                        socket.Send(data.ToArray());
                    }
                    catch (SocketException e)
                    {
                        if (e.NativeErrorCode.Equals(10060))
                            timedOut = true;
                        //else
                        //    throw e;
                    }
                }
            }
            return input;
        }

        public static IResourceProvider<NetSocket> SendTo(IResourceProvider<NetSocket> input, int timeOut, IPAddress remoteAddress, int remotePort, IEnumerable<byte> data, bool send, out bool timedOut)
        {
            timedOut = false;
            if (send && input != null)
            {
                using (var h = input.GetHandle())
                {
                    var socket = h.Resource;
                    try
                    {
                        socket.SendTimeout = timeOut;
                        socket.SendTo(data.ToArray(), new IPEndPoint(remoteAddress, remotePort));
                    }
                    catch (SocketException e)
                    {
                        if (e.NativeErrorCode.Equals(10060))
                            timedOut = true;
                        //else
                        //    throw e;
                    }
                }
            }
            return input;
        }

        public static IResourceProvider<NetSocket> ReceiveFrom(IResourceProvider<NetSocket> input, int timeOut, out Spread<byte> data, out IPEndPoint remote, out bool timedOut)
        {
            remote = NetUtils.DefaultIPEndPoint;
            timedOut = false;
            var s = new SpreadBuilder<byte>();

            if (input != null)
            {
                using (var h = input.GetHandle())
                {
                    var socket = h.Resource;
                    var none = socket.Connected;
                    if ((socket != null) && socket.IsBound)  //&& socket.Connected)
                    {
                        var b = new byte[socket.ReceiveBufferSize];
                        try
                        {
                            socket.ReceiveTimeout = timeOut;
                            EndPoint ep = NetUtils.DefaultIPEndPoint;
                            var c = socket.ReceiveFrom(b, ref ep);
                            remote = (IPEndPoint)ep;
                            if (c > 0)
                                s.AddRange(b, c);
                        }
                        catch (SocketException e)
                        {
                            if (e.NativeErrorCode.Equals(10060)) //timedout
                                timedOut = true;
                            //else if (!e.NativeErrorCode.Equals(10035)) //WSAEWOULDBLOCK
                            //    throw e;
                        }
                    }
                }
            }
            data = s.ToSpread();
            return input;
        }

        public static IResourceProvider<NetSocket> CheckConnected(IResourceProvider<NetSocket> input, out bool closed)
        {
            closed = true;
            if (input != null)
            {
                using (var h = input.GetHandle())
                {
                    closed = CheckIfClosed(h.Resource);
                }
            }
            return input;
        }

        public static bool CheckIfClosed(NetSocket socket)
        {
            bool closed = false;
            var none = socket.Connected; //need this, to have valid IsBound 
            if (socket.IsBound)
            {
                var count = 1000;
                bool blockingState = socket.Blocking;
                try
                {
                    byte[] tmp = new byte[1];
                    socket.Blocking = false;
                    count = socket.Receive(tmp, SocketFlags.Peek);
                    if (count == 0)
                        closed = true;
                }
                catch (SocketException e)
                {
                    if (!e.NativeErrorCode.Equals(10035)) // 10035 == WSAEWOULDBLOCK
                        closed = true;
                }
                catch (Exception)
                {
                }
                finally
                {
                    try
                    {
                        socket.Blocking = blockingState;
                    }
                    catch { }
                }
            }
            return closed;
        }
    }
}

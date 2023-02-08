using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Lib.Experimental.ProcessGraph
{
    public interface IPGPin
    {
        bool IsSource { get; }
        void AcknowledgeConnectionChange(ConnectionChangeInfo info);
        //bool CanConnectTo(IPGPin otherPin);
    }

    public struct ConnectionChangeInfo
    {
        public readonly bool GotConnected;
        public readonly IPGPin Source;
        public readonly IPGPin Sink;

        public ConnectionChangeInfo(IPGPin source, IPGPin sink, bool connect)
        {
            GotConnected = connect;
            Source = source;
            Sink = sink;
        }

        public override string ToString()
        {
            if (GotConnected)
                return string.Format("{0} got connected to {1}", Source, Sink);
            else
                return string.Format("{0} got disconnected from {1}", Source, Sink);
        }
    }

    public interface IPGNode
    {
        //bool CanConnectTo(IPGPin ourPin, IPGPin otherPin);
    }

    public static class ConnectionHelpers
    {
        public static void Connect(ConnectionChangeInfo info)
        {
            info.Source.AcknowledgeConnectionChange(info);
            info.Sink.AcknowledgeConnectionChange(info);
        }

        public static void ConnecTo(this IPGPin ourSink, IPGPin otherSource)
        {
            var info = new ConnectionChangeInfo(otherSource, ourSink, true);
            Connect(info);
        }

        public static void DisConnectFrom(this IPGPin ourSink, IPGPin otherSource)
        {
            var info = new ConnectionChangeInfo(otherSource, ourSink, false);
            Connect(info);
        }
    }
}

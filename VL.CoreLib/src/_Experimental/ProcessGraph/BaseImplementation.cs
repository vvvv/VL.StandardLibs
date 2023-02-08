using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Lib.Experimental.ProcessGraph
{
    public class StdPGPin : IPGPin
    {
        public StdPGNode owner;
        public bool isSource;

        public StdPGPin(StdPGNode owner, bool isSource)
        {
            this.owner = owner;
            this.isSource = isSource;
        }

        public bool IsSource { get { return this.isSource; } }

        public void AcknowledgeConnectionChange(ConnectionChangeInfo info)
        {
            owner.AcknowledgeConnectionChange(info);
        }

        //public bool CanConnectTo(IPGPin otherPin)
        //{
        //    return owner.CanConnectTo(this, otherPin);
        //}
    }

    public abstract class StdPGNode : IPGNode
    {
        //public readonly List<StdPGPin> Inputs = new List<StdPGPin>();
        //public readonly List<StdPGPin> Outputs = new List<StdPGPin>();

        public virtual void AcknowledgeConnectionChange(ConnectionChangeInfo info)
        {
        }

        //public abstract bool CanConnectTo(IPGPin ourPin, IPGPin otherPin);  
    }
}

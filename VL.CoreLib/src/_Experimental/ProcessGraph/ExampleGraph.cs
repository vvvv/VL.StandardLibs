using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Lib.Experimental.ProcessGraph
{
    public class ASource : StdPGPin
    {
        public ASource(StdPGNode node)
            : base(node, true)
        {
        }
    }

    public class ASink : StdPGPin
    {
        public ASink(StdPGNode node)
            : base(node, false)
        {
        }

        ASource source;
        public ASource Source
        {
            get { return source; }
            set {                
                if (value != this.source)
                {
                    if (this.source != null)
                        this.DisConnectFrom(this.source);
                    this.source = value;
                    if (this.source != null)
                        this.ConnecTo(this.source);
                }
            }
        }
    }

    public class MyNode1 : StdPGNode
    {
        ASink input;
        ASink input2;
        ASource output;
        ASource output2;

        public MyNode1()
        {
            input = new ASink(this);
            input2 = new ASink(this);
            output = new ASource(this);
            output2 = new ASource(this);
        }

        public void Update(ASource input, ASource input2, out ASource output, out ASource output2)
        {
            this.input.Source = input;
            this.input2.Source = input2;


            output = this.output;
            output2 = this.output2;
        }

        public override void AcknowledgeConnectionChange(ConnectionChangeInfo info)
        {
            if (info.Sink == input || info.Sink == input2)
                Trace.WriteLine("upstream action: " + info);
            else
                Trace.WriteLine("downstream action: " + info);
        }

        //public override bool CanConnectTo(IPGPin pin1, IPGPin pin2)
        //{
        //    return true;
        //}
    }
}

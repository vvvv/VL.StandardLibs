using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VL.Lib.IO.Net
{
    public static class IPAddressNodes
    {
        public static bool Equals(IPAddress input, IPAddress input2)
        {
            if (ReferenceEquals(input, input2))
                return true;
            if (ReferenceEquals(input, null))
                return false;
            return input.Equals(input2);
        }
    }

    public static class IPEndPointNodes
    {
        public static bool Equals(IPEndPoint input, IPEndPoint input2)
        {
            if (ReferenceEquals(input, input2))
                return true;
            if (ReferenceEquals(input, null))
                return false;
            return input.Equals(input2);
        }
    }
}

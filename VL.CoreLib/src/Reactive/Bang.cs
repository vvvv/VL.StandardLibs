using System;
using System.Threading;

namespace VL.Lib.Reactive
{
    public struct Bang : IEquatable<Bang>
    {
        public static Bang Trigger => new Bang();

        static int someNumber = 0;
        readonly int distinction = Interlocked.Increment(ref someNumber);

        public Bang()
        {
        }

        public bool Equals(Bang other)
        {
            return distinction == other.distinction;
        }

        public override bool Equals(object obj)
        {
            return obj is Bang && Equals((Bang)obj);
        }

        public override int GetHashCode()
        {
            return distinction.GetHashCode();
        }
    }
}

#nullable enable
using System.Collections.Generic;

namespace VL.Core
{
    public sealed class PinDescriptionComparer : IEqualityComparer<IVLPinDescription>
    {
        public static readonly PinDescriptionComparer Default = new PinDescriptionComparer();

        public bool Equals(IVLPinDescription? x, IVLPinDescription? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            return x.Name == y.Name && x.Type == y.Type && Equals(x.DefaultValue, y.DefaultValue);
        }

        public int GetHashCode(IVLPinDescription obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
#nullable restore
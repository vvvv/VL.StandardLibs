#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace VL.Core
{
    public sealed class NodeDescriptionComparer : IEqualityComparer<IVLNodeDescription>
    {
        public static readonly NodeDescriptionComparer Default = new NodeDescriptionComparer();

        public bool Equals(IVLNodeDescription? x, IVLNodeDescription? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;

            return x.Name == y.Name
                && x.Category == y.Category
                && x.Inputs.SequenceEqual(y.Inputs, PinDescriptionComparer.Default)
                && x.Outputs.SequenceEqual(y.Outputs, PinDescriptionComparer.Default);
        }

        public int GetHashCode(IVLNodeDescription obj)
        {
            return obj.Name.GetHashCode() ^ obj.Category.GetHashCode();
        }
    }
}
#nullable restore
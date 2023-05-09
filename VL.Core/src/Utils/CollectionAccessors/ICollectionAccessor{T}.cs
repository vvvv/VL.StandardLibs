using System.Collections.Generic;

namespace VL.Core.Utils
{
    public interface ICollectionAccessor<T> : IList<T>, ICollectionAccessor
    {
    }
}

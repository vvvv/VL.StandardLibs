using System;

namespace VL.Core
{
    public abstract class NodeFactoryCache
    {
        public abstract IVLNodeDescriptionFactory GetOrAdd(string identifier, Func<IVLNodeDescriptionFactory> factory);
    }
}

using System.Collections.Generic;

namespace VL.Core
{
    /// <summary>
    /// Easy to use base class to implement <see cref="IVLNode"/>.
    /// </summary>
    public abstract class FactoryBasedVLNode : IVLObject
    {
        public FactoryBasedVLNode(NodeContext context)
            : this(AppHost.CurrentOrGlobal, context)
        {
            Context = context;
        }

        public FactoryBasedVLNode(AppHost appHost, NodeContext context)
        {
            AppHost = appHost;
            Context = context;
        }

        public AppHost AppHost { get; }

        public NodeContext Context { get; }

        uint IVLObject.Identity => 0;

        IVLObject IVLObject.With(IReadOnlyDictionary<string, object> values)
        {
            return this;
        }
    }
}

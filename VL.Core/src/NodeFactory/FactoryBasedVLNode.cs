using System.Collections.Generic;

namespace VL.Core
{
    /// <summary>
    /// Easy to use base class to implement <see cref="IVLNode"/>.
    /// </summary>
    public abstract class FactoryBasedVLNode : IVLObject
    {
        public FactoryBasedVLNode(NodeContext context)
            : this(ServiceRegistry.CurrentOrGlobal, context)
        {
            Context = context;
        }

        public FactoryBasedVLNode(ServiceRegistry services, NodeContext context)
        {
            Services = services;
            Context = context;
        }

        public ServiceRegistry Services { get; }

        public NodeContext Context { get; }

        IVLTypeInfo IVLObject.Type => TypeRegistry.Default.GetTypeInfo(GetType());

        uint IVLObject.Identity => 0;

        IVLObject IVLObject.With(IReadOnlyDictionary<string, object> values)
        {
            return this;
        }
    }
}

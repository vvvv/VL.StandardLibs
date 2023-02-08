using System;
using System.Collections.Generic;

namespace VL.Core
{
    [Obsolete($"Inherit from {nameof(FactoryBasedVLNode)} - this class only exists to keep API compatibility", error: true)]
    public abstract class VLObject : IVLObject
    {
        public VLObject(NodeContext context)
            : this(ServiceRegistry.CurrentOrGlobal, context)
        {
            Context = context;
        }

        public VLObject(ServiceRegistry services, NodeContext context)
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

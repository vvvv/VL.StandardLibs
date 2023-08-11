using System;
using System.Collections.Generic;

namespace VL.Core
{
    [Obsolete($"Inherit from {nameof(FactoryBasedVLNode)} - this class only exists to keep API compatibility", error: true)]
    public abstract class VLObject : IVLObject
    {
        public VLObject(AppHost appHost, NodeContext context)
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

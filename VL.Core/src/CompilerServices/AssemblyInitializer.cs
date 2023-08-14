using System;
using System.Reflection;

namespace VL.Core.CompilerServices
{
    public abstract class AssemblyInitializer
    {
        internal const string DefaultFieldName = "Default";

        internal virtual bool ContainsUserCode => false;

        internal Assembly Assembly => this.GetType().Assembly;

        // Overwritten by target code
        public virtual void CollectDependencies(DependencyCollector collector)
        {
            collector.AddDependency(this);
        }

        public virtual void Configure(AppHost appHost)
        {
            // For backwards compatibility
            RegisterServices(appHost.Factory);
        }

        protected virtual void RegisterServices(IVLFactory factory)
        {

        }
    }

    public abstract class AssemblyInitializer<TImpl> : AssemblyInitializer
        where TImpl : AssemblyInitializer<TImpl>, new()
    {
        public static readonly TImpl Default = new TImpl();
    }
}

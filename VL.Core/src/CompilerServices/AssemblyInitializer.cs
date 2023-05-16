using System.ComponentModel;

namespace VL.Core.CompilerServices
{
    public abstract class AssemblyInitializer
    {
        public const string RegisterServicesMethodName = nameof(RegisterServices);
        public const string DefaultFieldName = "Default";

        // Only called by emitted code
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Init(IVLFactory factory) => factory.AppHost.RegisterServices(this);

        internal virtual void RegisterServices(AppHost appHost)
        {
            RegisterServices(appHost.Services.GetService<IVLFactory>());
        }

        protected abstract void RegisterServices(IVLFactory factory);
    }

    public abstract class AssemblyInitializer<TImpl> : AssemblyInitializer
        where TImpl : AssemblyInitializer<TImpl>, new()
    {
        public static readonly TImpl Default = new TImpl();
    }
}

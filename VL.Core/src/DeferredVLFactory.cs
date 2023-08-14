using System;

namespace VL.Core
{
    internal sealed class DeferredVLFactory : IVLFactory
    {
        public static readonly IVLFactory Default = new DeferredVLFactory(() => AppHost.Current.Factory);

        private readonly Func<IVLFactory> lazyFactory;

        public DeferredVLFactory(Func<IVLFactory> factory)
        {
            lazyFactory = factory;
        }

        private IVLFactory Factory => lazyFactory();

        public AppHost AppHost => Factory.AppHost;

        [Obsolete("Please use TypeUtils.New")]
        public object CreateInstance(Type type, NodeContext nodeContext)
        {
            return Factory.CreateInstance(type, nodeContext);
        }

        [Obsolete("Please use TypeUtils.Default")]
        public object GetDefaultValue(Type type)
        {
            return Factory.GetDefaultValue(type);
        }

        public Func<object, object> GetServiceFactory(Type forType, Type serviceType)
        {
            return Factory.GetServiceFactory(forType, serviceType);
        }

        public Type GetTypeByName(string name)
        {
            return Factory.GetTypeByName(name);
        }

        public IVLTypeInfo GetTypeInfo(Type type)
        {
            return Factory.GetTypeInfo(type);
        }

        public void RegisterService(Type forType, Type serviceType, Func<object, object> serviceFactory)
        {
            Factory.RegisterService(forType, serviceType, serviceFactory);
        }
    }
}

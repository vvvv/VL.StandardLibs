using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace VL.Core.CompilerServices
{
    public abstract class AssemblyInitializer
    {
        public const string RegisterServicesMethodName = nameof(RegisterServices);
        public const string DefaultFieldName = "Default";

        [ThreadStatic]
        internal static Assembly CurrentAssembly;

        // Only called by emitted code
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Init(IVLFactory factory) => Init(factory.AppHost);

        public void Init(AppHost appHost)
        {
            try
            {
                var factory = appHost.Services.GetService<IVLFactory>();
                // Internal factory might have seen us already
                if (factory is IInternalVLFactory internalFactory)
                    internalFactory.Initialize(this);
                else
                    DoRegister(appHost);
            }
            catch (Exception e)
            {
                throw new InitializationException(e);
            }
        }

        internal void InitInternalFactory(AppHost appHost)
        {
            try
            {
                DoRegister(appHost);
            }
            catch (Exception e)
            {
                throw new InitializationException(e);
            }
        }

        private void DoRegister(AppHost appHost)
        {
            var current = CurrentAssembly;
            CurrentAssembly = this.GetType().Assembly;
            try
            {
                RegisterServices(appHost.Services.GetService<IVLFactory>());
            }
            catch (Exception e)
            {
                RuntimeGraph.ReportException(e);
            }
            finally
            {
                CurrentAssembly = current;
            }
        }

        protected abstract void RegisterServices(IVLFactory factory);
    }

    public abstract class AssemblyInitializer<TImpl> : AssemblyInitializer
        where TImpl : AssemblyInitializer<TImpl>, new()
    {
        public static readonly TImpl Default = new TImpl();
    }
}

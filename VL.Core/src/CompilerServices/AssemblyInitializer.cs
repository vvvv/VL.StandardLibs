using System;
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

        public void Init(IVLFactory factory)
        {
            try
            {
                // Internal factory might have seen us already
                if (factory is IInternalVLFactory internalFactory)
                    internalFactory.Initialize(this);
                else
                    DoRegister(factory);
            }
            catch (Exception e)
            {
                throw new InitializationException(e);
            }
        }

        internal void Prepare()
        {
            RuntimeHelpers.PrepareMethod(GetType().GetMethod(nameof(RegisterServices), BindingFlags.Instance | BindingFlags.NonPublic).MethodHandle);
        }

        internal void InitInternalFactory(IInternalVLFactory factory)
        {
            try
            {
                DoRegister(factory);
            }
            catch (Exception e)
            {
                throw new InitializationException(e);
            }
        }

        private void DoRegister(IVLFactory factory)
        {
            var current = CurrentAssembly;
            CurrentAssembly = this.GetType().Assembly;
            try
            {
                RegisterServices(factory);
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

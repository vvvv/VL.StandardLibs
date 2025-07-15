using MBrace.FsPickler;
using MBrace.FsPickler.CSharpProxy;
using System;
using System.Collections;
using VL.Serialization.FSPickler.FSharp;
using VL.Core;

namespace VL.Serialization.FSPickler
{
    sealed class PicklerResolver : IPicklerResolver, ICustomPicklerRegistry
    {
        public static readonly PicklerResolver Instance = new PicklerResolver();

        private readonly IPicklerResolver resolver;

        private PicklerResolver()
        {
            resolver = PicklerCache.FromCustomPicklerRegistry(this);
            IHotswapSpecificNodes.Impl.ProgramInstantiated += (s, e) =>
            {
                // Sadly a Clear method is not exposed and creating new caches causes an exception after a certain time
                var cacheField = typeof(PicklerCache).GetField("dict", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var cache = cacheField!.GetValue(resolver) as IDictionary;
                cache!.Clear();
            };
        }

        public CustomPicklerRegistration GetRegistration(Type type)
        {
            return CustomPicklerRegistration.UnRegistered;
        }

        public bool IsSerializable(Type value)
        {
            return resolver.IsSerializable(value);
        }

        public bool IsSerializable<T>()
        {
            return resolver.IsSerializable<T>();
        }

        public Pickler Resolve(Type value)
        {
            return resolver.Resolve(value);
        }

        public Pickler<T> Resolve<T>() => (Pickler<T>)Resolve(typeof(T));
    }
}

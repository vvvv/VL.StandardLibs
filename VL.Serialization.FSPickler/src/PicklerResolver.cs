using MBrace.FsPickler;
using MBrace.FsPickler.CSharpProxy;
using System;
using System.Collections;
using VL.Serialization.FSPickler.FSharp;
using VL.Core;
using System.Collections.Generic;
using VL.Core.Utils;

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
            var typeInfo = AppHost.CurrentOrGlobal.TypeRegistry.GetTypeInfo(type);
            if (typeInfo.IsPatched)
            {
                var method = typeof(PicklerResolver)
                    .GetMethod(nameof(PicklerResolver.CreateVLObjectPickler), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    !.MakeGenericMethod(typeInfo.ClrType);

                Func<IPicklerResolver, Pickler> x = resolver => (Pickler)method.Invoke(null, new object[] { resolver })!;
                return CustomPicklerRegistration.NewCustomPickler(x.ToFSharpFunc());
            }

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

        static Pickler<TPublicType> CreateVLObjectPickler<TPublicType>(IPicklerResolver resolver)
            where TPublicType : IVLObject
        {
            return PicklerFactory.CreatePickler(
                reader: rs =>
                {
                    var appHost = AppHost.CurrentOrGlobal;
                    var instance = appHost.CreateInstance<TPublicType>();
                    if (instance is null)
                        throw new InvalidOperationException($"Cannot create instance of {typeof(TPublicType).FullName}.");

                    var typeInfo = AppHost.CurrentOrGlobal.TypeRegistry.GetTypeInfo(typeof(TPublicType));
                    var pool = DictionaryPool<string, object>.Default;
                    var values = pool.Rent();
                    try
                    {
                        foreach (var p in typeInfo.Properties)
                        {
                            if (!p.ShouldBeSerialized)
                                continue;

                            var r = resolver.Resolve(p.Type.ClrType);
                            var v = r.UntypedRead(rs, p.NameForTextualCode);
                            values.Add(p.NameForTextualCode, v);
                        }
                        return (TPublicType)instance.With(values);
                    }
                    finally
                    {
                        pool.Return(values);
                    }
                },
                writer: (ws, obj) =>
                {
                    var typeInfo = AppHost.CurrentOrGlobal.TypeRegistry.GetTypeInfo(typeof(TPublicType));
                    foreach (var p in typeInfo.Properties)
                    {
                        if (!p.ShouldBeSerialized)
                            continue;

                        var r = resolver.Resolve(p.Type.ClrType);
                        r.UntypedWrite(ws, p.NameForTextualCode, p.GetValue(obj));
                    }
                },
                useWithSubtypes: true);
        }
    }
}

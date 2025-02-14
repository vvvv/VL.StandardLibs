using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using VL.Core;
using VL.Lib.Collections;
using VL.Serialization.MessagePack.Formatters;

namespace VL.Serialization.MessagePack.Resolvers
{

    sealed class VLResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static IFormatterResolver Instance { get { return lazyFormatter.Value; } }

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static MessagePackSerializerOptions Options { get { return lazyOptions.Value; } }


        private static readonly Lazy<IFormatterResolver> lazyFormatter = new Lazy<IFormatterResolver>(() => new VLResolver());
        private static readonly Lazy<MessagePackSerializerOptions> lazyOptions = new Lazy<MessagePackSerializerOptions>(() => new MessagePackSerializerOptions(lazyFormatter.Value));

        // configure your custom resolvers.
        private static readonly IFormatterResolver[] Resolvers =
        [
            CompositeResolver.Create(
                [TypelessFormatter.Instance],
                [
                    StrideResolver.Instance, 
                    SkiaResolver.Instance, 
                    StandardResolver.Instance,
                    TypelessObjectResolver.Instance
                ])
        ];

        private readonly ResolverCache resolverCache = new ResolverCache(Resolvers);

        public IMessagePackFormatter<T>? GetFormatter<T>() => this.resolverCache.GetFormatter<T>();

        private class ResolverCache : CachingFormatterResolver
        {
            private readonly IReadOnlyList<IFormatterResolver> resolvers;

            internal ResolverCache(IReadOnlyList<IFormatterResolver> resolvers)
            {
                this.resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
            }

            protected override IMessagePackFormatter<T>? GetFormatterCore<T>()
            {
                if(!typeof(T).IsInterface && typeof(IVLObject).IsAssignableFrom(typeof(T)))
                {
                    return new IVLObjectFormatter<T>(AppHost.Current);
                }
                else if (!typeof(T).IsInterface && typeof(ISpread).IsAssignableFrom(typeof(T)))
                {

                    var genericTypeArgument = typeof(T).GetGenericArguments()[0];
                    return (IMessagePackFormatter<T>?)Activator.CreateInstance(typeof(SpreadFormatter<>).MakeGenericType(genericTypeArgument));
                }
                else if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Optional<>))
                {
                    var genericTypeArgument = typeof(T).GetGenericArguments()[0];
                    return (IMessagePackFormatter<T>?)Activator.CreateInstance(typeof(OptionalFormatter<>).MakeGenericType(genericTypeArgument));
                }
                else if (typeof(T).IsAssignableTo(typeof(IDynamicEnum)))
                {
                    return (IMessagePackFormatter<T>?)Activator.CreateInstance(typeof(DynamicEnumFormatter<>).MakeGenericType(typeof(T)));
                }

                foreach (IFormatterResolver item in this.resolvers)
                {
                    IMessagePackFormatter<T>? f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        return f;
                    }
                }

                if (typeof(T).IsAbstract)
                {
                    return new AbstractTypeFormatter<T>();
                }

                return null;
            }
        }

        private class AbstractTypeFormatter<T> : IMessagePackFormatter<T>
        {
            private readonly IMessagePackFormatter<object?> typelessFormatter = TypelessFormatter.Instance;

            public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
            {
                typelessFormatter.Serialize(ref writer, value, options);
            }

            public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                return (T)typelessFormatter.Deserialize(ref reader, options)!;
            }
        }
    }
}

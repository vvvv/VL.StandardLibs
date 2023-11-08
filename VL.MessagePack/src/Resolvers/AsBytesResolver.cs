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
using VL.MessagePack.Formatters;

namespace VL.MessagePack.Resolvers
{

    public sealed class AsBytesResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static IFormatterResolver Instance { get { return lazyFormatter.Value; } }

        /// <summary>
        /// A <see cref="MessagePackSerializerOptions"/> instance with this formatter pre-configured.
        /// </summary>
        public static MessagePackSerializerOptions Options { get { return lazyOptions.Value; } }


        private static readonly Lazy<IFormatterResolver> lazyFormatter = new Lazy<IFormatterResolver>(() => new AsBytesResolver());
        private static readonly Lazy<MessagePackSerializerOptions> lazyOptions = new Lazy<MessagePackSerializerOptions>(() => new MessagePackSerializerOptions(lazyFormatter.Value));


        private readonly ResolverCache resolverCache = new ResolverCache();

        public IMessagePackFormatter<T>? GetFormatter<T>() => this.resolverCache.GetFormatter<T>();

        private class ResolverCache : CachingFormatterResolver
        {

            internal ResolverCache()
            {
            }

            protected override IMessagePackFormatter<T>? GetFormatterCore<T>()
            {
                if(typeof(IVLObject).IsBlitable())
                {
                    return (IMessagePackFormatter<T>?)Activator.CreateInstance(typeof(StructAsByteFormatter<>).MakeGenericType(typeof(T)));
                }
                else if (typeof(ISpread).IsAssignableFrom(typeof(T)))
                {
                    var genericTypeArgument = typeof(T).GetGenericArguments()[0];
                    return  (IMessagePackFormatter<T>?)Activator.CreateInstance(typeof(SpreadAsByteFormatter<>).MakeGenericType(genericTypeArgument));
                }
                else if (typeof(T) == typeof(String))
                {
                    return (IMessagePackFormatter<T>)new StringAsByteFormatter();
                }
                return null;
            }
        }
    }
}

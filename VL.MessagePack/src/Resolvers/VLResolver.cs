using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using VL.Core;
using VL.MessagePack.Formatters;

namespace VL.MessagePack.Resolvers
{

    public sealed class VLResolver : IFormatterResolver
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
        private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]
        {
            StrideResolver.Instance,
            StandardResolver.Instance,
            TypelessObjectResolver.Instance, 
            //TypelessContractlessStandardResolver.Instance,
            //ContractlessStandardResolver.Instance
        };

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
                if (typeof(IVLObject).IsAssignableFrom(typeof(T)))
                {
                    return (IMessagePackFormatter<T>)new IVLObjectFormatter<T>(AppHost.Current);
                }

                foreach (IFormatterResolver item in this.resolvers)
                {
                    IMessagePackFormatter<T>? f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        return f;
                    }
                }

                return null;
            }
        }
    }
}

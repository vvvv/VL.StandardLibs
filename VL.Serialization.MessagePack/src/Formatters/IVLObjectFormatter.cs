using MessagePack.Formatters;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using VL.Core;
using VL.Serialization.MessagePack.Internal;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using System.Reflection;
using VL.Core.Utils;
using System.Collections.Immutable;
using VL.Serialization.MessagePack.Resolvers;

namespace VL.Serialization.MessagePack.Formatters
{

    sealed class IVLObjectFormatter<T> : IMessagePackFormatter<T>
    {
        private delegate void SerializeMethod(object dynamicContractlessFormatter, ref MessagePackWriter writer, object value, MessagePackSerializerOptions options);
        private delegate object DeserializeMethod(object dynamicContractlessFormatter, ref MessagePackReader reader, MessagePackSerializerOptions options);
        private static readonly ThreadsafeTypeKeyHashTable<SerializeMethod> Serializers = new();
        private static readonly ThreadsafeTypeKeyHashTable<DeserializeMethod> Deserializers = new();

        private readonly AppHost appHost;
        private readonly IVLFactory factory;
        private readonly IVLTypeInfo typeInfo;
        private readonly Dictionary<string, (IVLPropertyInfo p, SerializeMethod s, DeserializeMethod d, object f)> properties;

        public IVLObjectFormatter(AppHost appHost)
        {
            this.appHost = appHost;
            this.factory = appHost.Factory;
            this.typeInfo = factory.GetTypeInfo(typeof(T));

            var properties = new Dictionary<string, (IVLPropertyInfo p, SerializeMethod s, DeserializeMethod d, object f)>();
            foreach (var p in typeInfo.Properties)
            {
                if (!p.ShouldBeSerialized)
                    continue;

                var type = p.Type.ClrType;
                var f = VLResolver.Options.Resolver.GetFormatterDynamic(type);
                if (f is null)
                    continue;

                var s = GetOrAddSerializer(type);
                var d = GetOrAddDeserializer(type);
                properties.Add(p.NameForTextualCode, (p, s, d, f));
            }
            this.properties = properties;
        }

        private static SerializeMethod GetOrAddSerializer(Type type)
        {
            if (!Serializers.TryGetValue(type, out var serializeMethod))
            {
                TypeInfo ti = type.GetTypeInfo();

                Type formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
                ParameterExpression param0 = Expression.Parameter(typeof(object), "formatter");
                ParameterExpression param1 = Expression.Parameter(typeof(MessagePackWriter).MakeByRefType(), "writer");
                ParameterExpression param2 = Expression.Parameter(typeof(object), "value");
                ParameterExpression param3 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

                MethodInfo serializeMethodInfo = formatterType.GetRuntimeMethod("Serialize", new[] { typeof(MessagePackWriter).MakeByRefType(), type, typeof(MessagePackSerializerOptions) })!;

                MethodCallExpression body = Expression.Call(
                    Expression.Convert(param0, formatterType),
                    serializeMethodInfo,
                    param1,
                    ti.IsValueType ? Expression.Unbox(param2, type) : Expression.Convert(param2, type),
                    param3);

                serializeMethod = Expression.Lambda<SerializeMethod>(body, param0, param1, param2, param3).Compile();

                Serializers.TryAdd(type, serializeMethod);
            }
            return serializeMethod;
        }

        private static DeserializeMethod GetOrAddDeserializer(Type type)
        {
            if (!Deserializers.TryGetValue(type, out var deserializeMethod))
            {
                TypeInfo ti = type.GetTypeInfo();

                Type formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(type);
                ParameterExpression param0 = Expression.Parameter(typeof(object), "formatter");
                ParameterExpression param1 = Expression.Parameter(typeof(MessagePackReader).MakeByRefType(), "reader");
                ParameterExpression param2 = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

                MethodInfo deserializeMethodInfo = formatterType.GetRuntimeMethod("Deserialize", new[] { typeof(MessagePackReader).MakeByRefType(), typeof(MessagePackSerializerOptions) })!;

                MethodCallExpression deserialize = Expression.Call(
                    Expression.Convert(param0, formatterType),
                    deserializeMethodInfo,
                    param1,
                    param2);

                Expression body = deserialize;
                if (ti.IsValueType)
                {
                    body = Expression.Convert(deserialize, typeof(object));
                }

                deserializeMethod = Expression.Lambda<DeserializeMethod>(body, param0, param1, param2).Compile();

                Deserializers.TryAdd(type, deserializeMethod);
            }
            return deserializeMethod;
        }

        public void Serialize(
          ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            var obj = value as IVLObject;
            if (obj is null)
            {
                writer.WriteNil();
                return;
            }

            // Write all properties as dict
            writer.WriteMapHeader(properties.Count);
            foreach (var (key, x) in properties)
            {
                writer.Write(key);
                x.s(x.f, ref writer, x.p.GetValue(obj), options);
            }
        }

        public T Deserialize(
          ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return default!;

            var propCount = reader.ReadMapHeader();
            options.Security.DepthStep(ref reader);

            using var values = Pooled.GetDictionary<string, object>();
            for (int i = 0; i < propCount; i++)
            {
                var key = reader.ReadString();
                if (key is null)
                    continue;

                if (properties.TryGetValue(key, out var x))
                {
                    var obj = x.d(x.f, ref reader, options);
                    values.Value.Add(key, obj);
                }
            }

            reader.Depth--;

            var instance = appHost.CreateInstance(typeInfo) as IVLObject;
            return (T)instance?.With(values.Value)!;
        }
    }
}

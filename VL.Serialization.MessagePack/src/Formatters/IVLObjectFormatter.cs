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

namespace VL.Serialization.MessagePack.Formatters
{

    sealed class IVLObjectFormatter<T> : IMessagePackFormatter<T>
    {
        private delegate void SerializeMethod(object dynamicContractlessFormatter, ref MessagePackWriter writer, object value, MessagePackSerializerOptions options);
        private delegate object DeserializeMethod(object dynamicContractlessFormatter, ref MessagePackReader reader, MessagePackSerializerOptions options);
        private static readonly ThreadsafeTypeKeyHashTable<SerializeMethod> Serializers = new();
        private static readonly ThreadsafeTypeKeyHashTable<DeserializeMethod> Deserializers = new();

        //private readonly ConcurrentDictionary<string,Type> propertyTyps = new ConcurrentDictionary<string,Type>();

        private readonly AppHost appHost;
        private readonly IVLFactory factory;
        private readonly IVLTypeInfo typeInfo;

        private readonly Regex removeCategories = new Regex(@"\s\[.+?\]", RegexOptions.Compiled);

        public IVLObjectFormatter(AppHost appHost)
        {
            this.appHost = appHost;
            this.factory = appHost.Factory;
            this.typeInfo = factory.GetTypeInfo(typeof(T));

        }

        private SerializeMethod? TryAddSerializers(Type type)
        {
            if (!Serializers.TryGetValue(type, out SerializeMethod? serializeMethod))
            {
                // double check locking...
                lock (Serializers)
                {
                    if (!Serializers.TryGetValue(type, out serializeMethod))
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
                }
            }
            return serializeMethod;
        }

        private DeserializeMethod? TryAddDeserializers(Type type)
        {
            if (!Deserializers.TryGetValue(type, out DeserializeMethod? deserializeMethod))
            {
                lock (Deserializers)
                {
                    if (!Deserializers.TryGetValue(type, out deserializeMethod))
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
                }
            }
            return deserializeMethod;
        }

        public void Serialize(
          ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }
            var properties = typeInfo.Properties;

            // Write all Propertys as Dict 
            writer.WriteMapHeader(properties.Count);
            foreach (var prop in properties)
            {
                writer.Write(prop.NameForTextualCode);
                var formatter = options.Resolver.GetFormatterDynamic(prop.Type.ClrType);
                SerializeMethod ? serializeMethod = TryAddSerializers(prop.Type.ClrType);
                if (serializeMethod != null && formatter != null)
                    serializeMethod(formatter, ref writer, prop.GetValue((IVLObject)value), options);

            }
        }

        public T Deserialize(
          ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default(T)!;
            }

            var instance = appHost.CreateInstance(typeInfo) as IVLObject;
            if (instance is null)
                return default!;

            int propCount = reader.ReadMapHeader();

            var builder = Pooled.GetDictionaryBuilder<string, object>();

            options.Security.DepthStep(ref reader);
            try
            {
                for (int i = 0; i < propCount; i++)
                {
                    string? key = reader.ReadString();
                    if (key != null)
                    {
                        var type = typeInfo.Properties.Where(prop => prop.NameForTextualCode == key).FirstOrDefault()?.Type.ClrType;

                        if (type != null)
                        {
                            var formatter = options.Resolver.GetFormatterDynamic(type);
                            DeserializeMethod? deserializeMethod = TryAddDeserializers(type);
                            if (deserializeMethod != null && formatter != null)
                            {
                                var obj = deserializeMethod(formatter, ref reader, options);
                                builder.Value.Add(key, obj);
                            }     
                        }
                    }
                }
            }
            finally
            {
                reader.Depth--;
            }

            return (T)instance.With(builder.ToImmutableAndFree());
        }
    }
}

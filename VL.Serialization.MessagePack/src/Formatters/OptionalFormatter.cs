using MessagePack.Formatters;
using MessagePack;
using VL.Core;

namespace VL.Serialization.MessagePack.Formatters
{
    sealed class OptionalFormatter<T> : IMessagePackFormatter<Optional<T>>
    {
        IMessagePackFormatter<T>? formatter = null;

        public void Serialize(ref MessagePackWriter writer, Optional<T> value, MessagePackSerializerOptions options)
        {
            if (value == default)
            {
                writer.WriteNil();
            }
            else
            {
                if (formatter == null)
                {
                    formatter = options.Resolver.GetFormatterWithVerify<T>();
                }

                formatter.Serialize(ref writer, value.Value, options);
            }
        }

        public Optional<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                if (formatter == null)
                {
                    formatter = options.Resolver.GetFormatterWithVerify<T>();
                }

                return formatter.Deserialize(ref reader, options);
            }
        }
    }
}

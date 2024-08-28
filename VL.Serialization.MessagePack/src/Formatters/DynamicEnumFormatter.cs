using System;
using MessagePack;
using MessagePack.Formatters;
using VL.Lib.Collections;

namespace VL.Serialization.MessagePack.Formatters
{
    sealed class DynamicEnumFormatter<T> : IMessagePackFormatter<T?>
        where T : IDynamicEnum
    {
        private readonly Func<string?, T> factory;

        public DynamicEnumFormatter()
        {
            var constructor = typeof(T).GetConstructor([typeof(string)]) ?? throw new InvalidOperationException($"Couldn't find constructor for {typeof(T)}");
            this.factory = v => (T)constructor.Invoke([v]);
        }

        public void Serialize(ref MessagePackWriter writer, T? value, MessagePackSerializerOptions options)
        {
            if (value is null)
                writer.WriteNil();
            else
                writer.Write(value.Value);
        }

        public T? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return default;

            return factory(reader.ReadString());
        }
    }
}

using MessagePack;
using System;
using System.Collections.Generic;
using VL.Serialization.MessagePack.Resolvers;

namespace VL.Serialization.MessagePack
{
    public static class MessagePackSerialization
    {
        public static byte[] Serialize<T>(T input)
        {
            return MessagePackSerializer.Serialize(input, VLResolver.Options);
        }

        public static T Deserialize<T>(IEnumerable<byte> input)
        {
            if (input.TryGetMemory(out var memory))
                return MessagePackSerializer.Deserialize<T>(memory, VLResolver.Options);

            throw new ArgumentException($"Couldn't retrieve memory from {typeof(T)}", nameof(input));
        }

        public static T Deserialize<T>(ReadOnlyMemory<byte> input)
        {
            return MessagePackSerializer.Deserialize<T>(input, VLResolver.Options);
        }

        public static string SerializeJson<T>(T input, bool prettify = true)
        {
            var json = MessagePackSerializer.SerializeToJson(input, VLResolver.Options);
            if (prettify)
                return JsonHelper.FormatJson(json);
            return json;
        }

        public static T DeserializeJson<T>(string input)
        {
            var bytes = MessagePackSerializer.ConvertFromJson(input, VLResolver.Options);
            return Deserialize<T>((ReadOnlyMemory<byte>)bytes);
        }
    }
}

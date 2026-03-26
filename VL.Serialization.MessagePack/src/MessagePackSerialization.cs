using MessagePack;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using VL.Core;
using VL.Core.Import;
using VL.Serialization.MessagePack.Resolvers;

namespace VL.Serialization.MessagePack
{
    [SkipCategory]
    public static class MessagePackSerialization
    {
        public static byte[] Serialize<T>(T input)
        {
            return MessagePackSerializer.Serialize(input, VLResolver.Options);
        }

        public static TWriter SerializeTo<TWriter, T>(TWriter writer, T input)
            where TWriter : IBufferWriter<byte>
        {
            MessagePackSerializer.Serialize(writer, input, VLResolver.Options);
            return writer;
        }

        public static TStream SerializeToStream<TStream, T>(TStream stream, T input)
            where TStream : Stream
        {
            MessagePackSerializer.Serialize(stream, input, VLResolver.Options);
            return stream;
        }

        public static T Deserialize<T>(IEnumerable<byte> input)
        {
            if (input.TryGetMemory(out var memory))
                return MessagePackSerializer.Deserialize<T>(memory, VLResolver.Options);

            throw new ArgumentException($"Couldn't retrieve memory from {typeof(T)}", nameof(input));
        }

        [Smell(SymbolSmell.Hidden)]
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

        public static TextWriter SerializeJsonTo<T>(T input, TextWriter writer)
        {
            MessagePackSerializer.SerializeToJson(writer, input, VLResolver.Options);
            return writer;
        }

        public static T DeserializeJson<T>(string input)
        {
            var bytes = MessagePackSerializer.ConvertFromJson(input, VLResolver.Options);
            return Deserialize<T>((ReadOnlyMemory<byte>)bytes);
        }
    }
}

using MBrace.FsPickler;
using MBrace.FsPickler.Json;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using Microsoft.FSharp.Core;
using VL.Core;

namespace VL.Serialization.FSPickler
{
    public static class FSPicklerSerialization
    {
        static FSharpOption<ITypeNameConverter> TypeNameConverter => new FSharpOption<ITypeNameConverter>(AppHost.CurrentOrGlobal.Services.GetOrAddService<ITypeNameConverter>(s => new FSPickler.TypeNameConverter(s.GetRequiredService<TypeRegistry>())));
        static FSharpOption<IPicklerResolver> PicklerResolver => new FSharpOption<IPicklerResolver>(FSPickler.PicklerResolver.Instance);

        public static string SerializeXml<T>(T value, bool indent = false)
        {
            var serializer = new XmlSerializer(indent, TypeNameConverter, PicklerResolver);
            return serializer.PickleToString(value);
        }

        public static T DeserializeXml<T>(string serializedValue, bool indent = false)
        {
            var serializer = new XmlSerializer(indent, TypeNameConverter, PicklerResolver);
            return serializer.UnPickleOfString<T>(serializedValue);
        }

        public static string SerializeJson<T>(T value, bool indent = false, bool omitHeader = true)
        {
            var serializer = new JsonSerializer(indent, omitHeader, TypeNameConverter, PicklerResolver);
            return serializer.PickleToString(value);
        }

        public static T DeserializeJson<T>(string serializedValue, bool indent = false, bool omitHeader = true)
        {
            var serializer = new JsonSerializer(indent, omitHeader, TypeNameConverter, PicklerResolver);
            return serializer.UnPickleOfString<T>(serializedValue);
        }

        public static byte[] SerializeBinary<T>(T value, bool forceLittleEndian = false)
        {
            var serializer = new BinarySerializer(forceLittleEndian, TypeNameConverter, PicklerResolver);
            return serializer.Pickle(value);
        }

        public static T DeserializeBinary<T>(IEnumerable<byte> serializedValue, bool forceLittleEndian = false)
        {
            if (serializedValue.TryGetMemory(out var memory))
            {
                var stream = memory.AsStream();
                var serializer = new BinarySerializer(forceLittleEndian, TypeNameConverter, PicklerResolver);
                return serializer.Deserialize<T>(stream);
            }
            throw new ArgumentException($"Couldn't retrieve memory from {typeof(T)}", nameof(serializedValue));
        }


    }
}

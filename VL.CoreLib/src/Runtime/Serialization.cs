using MBrace.FsPickler;
using MBrace.FsPickler.CSharpProxy;
using MBrace.FsPickler.Json;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections;
using VL.Core;
using Microsoft.FSharp.Core;

namespace VL.Lib.Runtime
{
    public static class Serialization
    {
        static FSharpOption<ITypeNameConverter> TypeNameConverter => new FSharpOption<ITypeNameConverter>(AppHost.CurrentOrGlobal.Services.GetService<ITypeNameConverter>());
        static FSharpOption<IPicklerResolver> PicklerResolver => new FSharpOption<IPicklerResolver>(AppHost.CurrentOrGlobal.Services.GetService<IPicklerResolver>());

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

        [Obsolete("BSON format has been deprecated by Newtonsoft")]
        public static byte[] SerializeBson<T>(T value)
        {
            var serializer = new BsonSerializer(TypeNameConverter, PicklerResolver);
            return serializer.Pickle(value);
        }

        [Obsolete("BSON format has been deprecated by Newtonsoft")]
        public static T DeserializeBson<T>(byte[] serializedValue)
        {
            var serializer = new BsonSerializer(TypeNameConverter, PicklerResolver);
            return serializer.UnPickle<T>(serializedValue);
        }

        public static byte[] SerializeBinary<T>(T value, bool forceLittleEndian = false)
        {
            var serializer = new BinarySerializer(forceLittleEndian, TypeNameConverter, PicklerResolver);
            return serializer.Pickle(value);
        }

        public static T DeserializeBinary<T>(byte[] serializedValue, bool forceLittleEndian = false)
        {
            var serializer = new BinarySerializer(forceLittleEndian, TypeNameConverter, PicklerResolver);
            return serializer.UnPickle<T>(serializedValue);
        }

        public static T DeserializeBinary<T>(ReadOnlyMemory<byte> serializedValue, bool forceLittleEndian = false)
        {
            var stream = serializedValue.AsStream();
            var serializer = new BinarySerializer(forceLittleEndian, TypeNameConverter, PicklerResolver);
            return serializer.Deserialize<T>(stream);
        }

        
    }
}

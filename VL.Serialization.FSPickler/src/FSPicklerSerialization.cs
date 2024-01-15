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
        static FSharpOption<ITypeNameConverter> TypeNameConverter
        {
            get
            {
                var appHost = AppHost.CurrentOrGlobal;
                var typeNameConverter = appHost.Services.GetService<ITypeNameConverter>();
                if (typeNameConverter is null)
                {
                    typeNameConverter = new TypeNameConverter(appHost.TypeRegistry);
                    appHost.Services.RegisterService(typeNameConverter);
                }
                return new FSharpOption<ITypeNameConverter>(typeNameConverter);
            }
        }

        static FSharpOption<IPicklerResolver> PicklerResolver => new FSharpOption<IPicklerResolver>(FSPickler.PicklerResolver.Instance);

        public static string SerializeXml<T>(T input, bool indent = false)
        {
            var serializer = new XmlSerializer(indent, TypeNameConverter, PicklerResolver);
            return serializer.PickleToString(input);
        }

        public static T DeserializeXml<T>(string input, bool indent = false)
        {
            var serializer = new XmlSerializer(indent, TypeNameConverter, PicklerResolver);
            return serializer.UnPickleOfString<T>(input);
        }

        public static string SerializeJson<T>(T input, bool indent = false, bool omitHeader = true)
        {
            var serializer = new JsonSerializer(indent, omitHeader, TypeNameConverter, PicklerResolver);
            return serializer.PickleToString(input);
        }

        public static T DeserializeJson<T>(string input, bool indent = false, bool omitHeader = true)
        {
            var serializer = new JsonSerializer(indent, omitHeader, TypeNameConverter, PicklerResolver);
            return serializer.UnPickleOfString<T>(input);
        }

        public static byte[] SerializeBinary<T>(T input, bool forceLittleEndian = false)
        {
            var serializer = new BinarySerializer(forceLittleEndian, TypeNameConverter, PicklerResolver);
            return serializer.Pickle(input);
        }

        public static T DeserializeBinary<T>(IEnumerable<byte> input, bool forceLittleEndian = false)
        {
            if (input.TryGetMemory(out var memory))
            {
                var stream = memory.AsStream();
                var serializer = new BinarySerializer(forceLittleEndian, TypeNameConverter, PicklerResolver);
                return serializer.Deserialize<T>(stream);
            }
            throw new ArgumentException($"Couldn't retrieve memory from {typeof(T)}", nameof(input));
        }


    }
}

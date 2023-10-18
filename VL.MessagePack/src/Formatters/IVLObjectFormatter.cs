using MessagePack.Formatters;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using MessagePack.Resolvers;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using VL.MessagePack.Internal;
using System.Runtime.Serialization;

namespace VL.MessagePack.Formatters
{
    public class IVLObjectFormatter : IMessagePackFormatter<IVLObject?>
    {
        private readonly Dictionary<string, object> propertys = new Dictionary<string, object>();
        private readonly AppHost appHost;
        private readonly ThreadsafeTypeKeyHashTable<IMessagePackFormatter?> formatters = new();


        public IVLObjectFormatter(AppHost appHost)
        {
            this.appHost = appHost;
        }

        public void Serialize(
          ref MessagePackWriter writer, IVLObject? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            var type = value.Type;
            var prop = type.Properties;

            var keyFormatter = options.Resolver.GetFormatterWithVerify<string>();
            var valueFormatter = options.Resolver.GetFormatterWithVerify<object?>();

            //using (var scratchRental = new ArrayBufferWriter())
            //{
            //    MessagePackWriter scratchWriter = writer.Clone(scratchRental);
            //    // Write TypeName
            //    scratchWriter.Write(type.Name);

            //    // Write all Propertys as Dict 
            //    scratchWriter.WriteMapHeader(prop.Count);
            //    foreach (var p in prop)
            //    {
            //        keyFormatter.Serialize(ref scratchWriter, p.NameForTextualCode, options);
            //        valueFormatter.Serialize(ref scratchWriter, p.GetValue(value), options);
            //    }

            //    scratchWriter.Flush();

            //    // mark as extension with code 100
            //    writer.WriteExtensionFormat(new ExtensionResult(100, scratchRental.OutputAsMemory));
            //}

            writer.WriteMapHeader(1);

            // Write TypeName
            writer.Write(type.Name);

            // Write all Propertys as Dict 
            writer.WriteMapHeader(prop.Count);
            foreach (var p in prop)
            {
                keyFormatter.Serialize(ref writer, p.NameForTextualCode, options);
                valueFormatter.Serialize(ref writer, p.GetValue(value), options);
            }
        }

        public IVLObject? Deserialize(
          ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            if (reader.ReadMapHeader() == 1)
            {
                var ivlType = reader.ReadString();
                int propCount = reader.ReadMapHeader();

                propertys.Clear();

                if (appHost != null)
                {
                    var factory = appHost.Factory;
                    if (factory != null)
                    {
                        var type = factory.GetTypeByName(ivlType);
                        if (type != null)
                        {
                            var typeinfo = factory.GetTypeInfo(type);
                            if (typeinfo != null)
                            {
                                IFormatterResolver resolver = options.Resolver;
                                IMessagePackFormatter<string> keyFormatter = resolver.GetFormatterWithVerify<string>();
                                IMessagePackFormatter<object> valueFormatter = resolver.GetFormatterWithVerify<object>();

                                IVLObject? instance = (IVLObject)appHost.CreateInstance(typeinfo);

                                options.Security.DepthStep(ref reader);
                                try
                                {
                                    for (int i = 0; i < propCount; i++)
                                    {
                                        string key = keyFormatter.Deserialize(ref reader, options);
                                        object value = valueFormatter.Deserialize(ref reader, options);
                                        propertys.Add(key, value);
                                    }
                                }
                                finally
                                {
                                    reader.Depth--;
                                }
                                return instance?.With(propertys);
                            }
                        }
                    }
                }
                return null;
            }
            else
            {
                return null;
            }
        }
    }
}

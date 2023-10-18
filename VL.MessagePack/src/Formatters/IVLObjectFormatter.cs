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

namespace VL.MessagePack.Formatters
{
    public class IVLObjectFormatter : IMessagePackFormatter<IVLObject?>
    {
        private readonly Dictionary<string, object> propertys = new Dictionary<string, object>();
        private readonly AppHost appHost;

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

            writer.WriteMapHeader(2);

            // Write TypeName
            writer.Write("Type");
            writer.Write(type.Name);

            // Write all Propertys as Dict 
            writer.Write("Properties");
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

            var ivlType   = reader.ReadString();
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
    }
}

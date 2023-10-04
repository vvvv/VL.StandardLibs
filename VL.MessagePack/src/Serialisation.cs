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

namespace VL.MessagePack
{
    public static class MsgPackEx
    {

    }

    public class IVLObjectFormatter : IMessagePackFormatter<IVLObject>
    {
        Dictionary<string, object> propertys = new Dictionary<string, object>();
        

        public void Serialize(
          ref MessagePackWriter writer, IVLObject value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            var type = value.Type;
            var prop = type.Properties;

            writer.Write(type.Name);

            // Write all Propertys as Dict
            writer.WriteMapHeader(prop.Count);
            foreach (var p in prop) 
            {
                //writer.WriteString((ReadOnlySpan<byte>)Encoding.ASCII.GetBytes(p.NameForTextualCode).AsSpan());
                writer.WriteString(MemoryMarshal.AsBytes(p.NameForTextualCode.AsSpan()));
                writer.WriteString(MemoryMarshal.AsBytes(p.GetVLTypeInfo().Name.AsSpan()));
                MessagePackSerializer.Serialize(p.GetType() ,ref writer, p.GetValue(value), options);
            }
        }

        public IVLObject Deserialize(
          ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var ivlType   = reader.ReadString();
            int propCount = reader.ReadMapHeader();

            propertys.Clear(); 

            var apphost = AppHost.CurrentOrGlobal;
            if (apphost == null)
            {
                var factory = AppHost.CurrentOrGlobal.Factory;
                if (factory == null)
                {
                    var type = factory.GetTypeByName(ivlType);
                    if (type != null)
                    {
                        var typeinfo = factory.GetTypeInfo(type);
                        if (typeinfo != null) 
                        {
                            for (int i = 0; i < propCount; i++)
                            {
                                var propName = reader.ReadString();
                                var propType = reader.ReadString();
                                var propValue = MessagePackSerializer.Deserialize(factory.GetTypeByName(propType), ref reader, options);
                                propertys.Add(propName, propValue);
                            }
                            IVLObject instance = (IVLObject)apphost.CreateInstance(typeinfo);
                            instance.With(propertys);
                            return instance;
                        }
                    }
                }
            }
            return null;
        }
    }

    //public class VLResolver : IFormatterResolver
    //{
    //    public static readonly IFormatterResolver Instance = new VLResolver();

    //    // configure your custom resolvers.
    //    private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]
    //    {
    //    };

    //    private VLResolver() { }

    //    public IMessagePackFormatter<T> GetFormatter<T>()
    //    {
    //        return Cache<T>.Formatter;
    //    }

    //    private static class Cache<T>
    //    {
    //        public static IMessagePackFormatter<T> Formatter;

    //        static Cache()
    //        {
    //            // configure your custom formatters.
    //            if (typeof(T) == typeof(XXX))
    //            {
    //                Formatter = new ICustomFormatter();
    //                return;
    //            }

    //            foreach (var resolver in Resolvers)
    //            {
    //                var f = resolver.GetFormatter<T>();
    //                if (f != null)
    //                {
    //                    Formatter = f;
    //                    return;
    //                }
    //            }
    //        }
    //    }
    //}

    static class MessagePack
    {


    }
}

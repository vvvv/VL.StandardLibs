using MessagePack.Formatters;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.Collections;
using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VL.Lib.IO;
using System.Buffers;
using System.Collections;
using CommunityToolkit.HighPerformance;
using VL.Core.Utils;

namespace VL.MessagePack.Formatters
{
    public class SpreadFormatter<T> : IMessagePackFormatter<Spread<T>?>
    {
        IMessagePackFormatter<T>? formatter = null;

        public void Serialize(ref MessagePackWriter writer, Spread<T>? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else if (value.IsEmpty)
            {
                writer.WriteArrayHeader(0);
            }
            else
            {
                if (formatter == null)
                {
                    formatter = options.Resolver.GetFormatterWithVerify<T>();
                }

                if (formatter != null)
                {
                    var span = value.AsSpan();
                    writer.WriteArrayHeader(span.Length);

                    for (int i = 0; i < span.Length; i++)
                    {
                        writer.CancellationToken.ThrowIfCancellationRequested();
                        formatter.Serialize(ref writer, span[i], options);
                    }
                }
            }
        }

        public Spread<T>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var len = reader.ReadArrayHeader();
                if (len == 0)
                {
                    return Spread<T>.Empty;
                }

                if (formatter == null)
                {
                    formatter = options.Resolver.GetFormatterWithVerify<T>();
                }

                if (formatter != null)
                {
                    var builder = Pooled.GetArrayBuilder<T>();
                    options.Security.DepthStep(ref reader);
                    try
                    {
                        for (int i = 0; i < len; i++)
                        {
                            builder.Add(formatter.Deserialize(ref reader, options));
                        }
                    }
                    finally
                    {
                        reader.Depth--;
                    }
                    return Spread.Create(builder.ToImmutableAndFree());
                }

                return Spread<T>.Empty;
            }
        }
    }

    public class SpreadAsByteFormatter<T> : IMessagePackFormatter<Spread<T>?> where T : unmanaged
    {
        public void Serialize(ref MessagePackWriter writer, Spread<T>? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else if (value.IsEmpty)
            {
                writer.WriteBinHeader(0);
            }
            else
            {
                var span = MemoryMarshal.AsBytes(value.AsSpan());
                writer.Write(span);
            }
        }

        public Spread<T>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var bytes = reader.ReadBytes() is ReadOnlySequence<byte> b ? b.FirstSpan : default;

                if (bytes.Length == 0)
                {
                    return Spread<T>.Empty;
                }
                else
                {
                    var span = MemoryMarshal.Cast<byte, T>(bytes);
                    
                    var builder =  Pooled.GetArrayBuilder<T>();
                    builder.Value.Count = span.Length;
                    span.CopyTo(builder.Value.AsSpan());

                    return Spread.Create<T>(builder.ToImmutableAndFree());
                    
                }
            }
        }
    }
}

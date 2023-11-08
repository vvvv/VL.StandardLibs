using MessagePack.Formatters;
using MessagePack;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VL.Core.Utils;
using VL.Lib.Collections;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using System.Collections.Immutable;

namespace VL.MessagePack.Formatters
{
    public unsafe class StringAsByteFormatter : IMessagePackFormatter<string> 
    {
        public void Serialize(ref MessagePackWriter writer, string value, MessagePackSerializerOptions options)
        {
            var chars = value.AsSpan();
            int count = UTF8Encoding.UTF8.GetByteCount(chars);
            var span = writer.GetSpan(count);
            writer.Advance(UTF8Encoding.UTF8.GetBytes(chars, span));
        }

        public string Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.Sequence.IsSingleSegment)
            {
                return UTF8Encoding.UTF8.GetString(reader.Sequence.FirstSpan);
            }
            return String.Empty;
        }
    }

    public unsafe class StructAsByteFormatter<T> : IMessagePackFormatter<T> where T : unmanaged
    {
        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            var span = writer.GetSpan(sizeof(T));
            Unsafe.As<byte, T>(ref span[0]) = value;
            writer.Advance(sizeof(T));
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var sequence = reader.ReadRaw(sizeof(T));
            if (sequence.IsSingleSegment)
            {
                return Unsafe.As<byte, T>(ref Unsafe.AsRef(sequence.FirstSpan[0]));
            }

            T answer;
            sequence.CopyTo(new Span<byte>(&answer, sizeof(T)));
            return answer;
        }
    }

    public unsafe class SpreadAsByteFormatter<T> : IMessagePackFormatter<Spread<T>> where T : unmanaged
    {
        public void Serialize(ref MessagePackWriter writer, Spread<T> value, MessagePackSerializerOptions options)
        {
            var byteCount = sizeof(T) * value.Count;

            var destinationSpan = writer.GetSpan(byteCount);
            fixed (void* destination = &destinationSpan[0])
            fixed (void* source = &value.GetInternalArray()[0])
            {
                Buffer.MemoryCopy(source, destination, byteCount, byteCount);
            }

            writer.Advance(byteCount);

            //var bytes = MemoryMarshal.AsBytes(value.AsSpan());
            //var span = writer.GetSpan(bytes.Length);
            //bytes.CopyTo(span);
            //writer.Advance(bytes.Length);
        }

        public Spread<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.Sequence.IsSingleSegment)
            {
                var span = MemoryMarshal.Cast<byte, T>(reader.Sequence.FirstSpan);
                var builder = Pooled.GetArrayBuilder<T>();
                builder.Value.Count = span.Length;
                span.CopyTo(builder.Value.AsSpan());
                return Spread.Create<T>(builder.ToImmutableAndFree());
            }
            return Spread<T>.Empty;
        }
    }
}

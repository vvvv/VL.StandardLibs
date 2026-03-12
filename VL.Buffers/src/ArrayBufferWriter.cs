using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using VL.Core;
using VL.Core.Import;

[assembly: ImportType(typeof(IBufferWriter<>), Category = "Buffers")]
[assembly: ImportType(typeof(IBuffer<>), Category = "Buffers")]
[assembly: ImportType(typeof(ArrayBufferWriter<>), Category = "Buffers")]
[assembly: ImportType(typeof(MemoryBufferWriter<>), Category = "Buffers")]

namespace VL.Buffers;

/// <summary>
/// Manages an instance of <see cref="ArrayBufferWriter{T}"/> into which data can be written.
/// Changing the capacity will create a new instance of the buffer writer.
/// </summary>
[ProcessNode(Name = "ArrayBufferWriter")]
public sealed class ArrayBufferWriterNode
{
    private Optional<int> capacity;
    private ArrayBufferWriter<byte>? bufferWriter;

    public ArrayBufferWriter<byte> Update(Optional<int> capacity)
    {
        if (bufferWriter is null || capacity != this.capacity)
        {
            this.capacity = capacity;
            bufferWriter = capacity.HasValue ? new ArrayBufferWriter<byte>(capacity.Value) : new ArrayBufferWriter<byte>();
        }
        return bufferWriter;
    }
}

/// <summary>
/// Manages an instance of <see cref="MemoryBufferWriter{T}"/> into which data can be written, backed by a <see cref="Memory{T}"/> instance.
/// </summary>
[ProcessNode(Name = "MemoryBufferWriter")]
public sealed class MemoryBufferWriterNode<T>
{
    private Memory<T> memory;
    private MemoryBufferWriter<T>? bufferWriter;
    public MemoryBufferWriter<T> Update(Memory<T> memory)
    {
        if (bufferWriter is null || !memory.Equals(this.memory))
        {
            this.memory = memory;
            bufferWriter = new MemoryBufferWriter<T>(memory);
        }
        return bufferWriter;
    }
}

[SkipCategory]
[Category("Buffers.IBufferWriter")]
public static class BufferWriterExtensions
{
    /// <inheritdoc cref="IBufferWriterExtensions.Write{T}(IBufferWriter{byte}, T)"/>
    public static TWriter Write<TWriter, T>(this TWriter writer, T value)
        where TWriter : IBufferWriter<byte>
        where T : unmanaged
    {
        IBufferWriterExtensions.Write(writer, value);
        return writer;
    }

    /// <inheritdoc cref="IBufferWriterExtensions.Write{T}(IBufferWriter{byte}, ReadOnlySpan{T})"/>
    public static TWriter Write<TWriter, T>(this TWriter writer, ReadOnlySpan<T> values)
        where TWriter : IBufferWriter<byte>
        where T : unmanaged
    {
        IBufferWriterExtensions.Write(writer, values);
        return writer;
    }

    /// <inheritdoc cref="IBufferWriterExtensions.Write{T}(IBufferWriter{byte}, ReadOnlySpan{T})"/>
    public static TWriter Write<TWriter, T>(this TWriter writer, T[] values)
        where TWriter : IBufferWriter<byte>
        where T : unmanaged
    {
        IBufferWriterExtensions.Write(writer, (ReadOnlySpan<T>)values);
        return writer;
    }

    /// <summary>
    /// Returns the data that has been written to the underlying buffer so far as an <see cref="ArraySegment{T}"/>.
    /// </summary>
    public static ArraySegment<T> WrittenSegment<T>(this IBufferWriter<T> writer)
    {
        if (writer is ArrayBufferWriter<T> arrayBufferWriter)
        {
            if (MemoryMarshal.TryGetArray(arrayBufferWriter.WrittenMemory, out var segment))
                return segment;
        }
        if (writer is IBuffer<T> buffer)
        {
            if (MemoryMarshal.TryGetArray(buffer.WrittenMemory, out var segment))
                return segment;
        }

        return ArraySegment<T>.Empty;
    }
}

[SkipCategory]
[Category("Buffers.ArrayBufferWriter")]
public static class ArrayBufferWriterExtensions
{
    /// <summary>
    /// Writes the specified value at the start of the provided buffer.
    /// </summary>
    public static ArrayBufferWriter<byte> WriteAtStart<T>(this ArrayBufferWriter<byte> writer, T value)
        where T : unmanaged
    {
        var count = writer.WrittenCount;
        writer.ResetWrittenCount();
        writer.Write(value);
        writer.Advance(Math.Max(0, count - writer.WrittenCount));
        return writer;
    }

    /// <inheritdoc cref="WriteAtStart{T}(ArrayBufferWriter{T}, ReadOnlySpan{T})"/>
    public static ArrayBufferWriter<T> WriteAtStart<T>(this ArrayBufferWriter<T> writer, T[] values)
    {
        return WriteAtStart(writer, (ReadOnlySpan<T>)values);
    }

    /// <summary>
    /// Writes the specified values at the start of the provided buffer.
    /// </summary>
    public static ArrayBufferWriter<T> WriteAtStart<T>(this ArrayBufferWriter<T> writer, ReadOnlySpan<T> values)
    {
        var count = writer.WrittenCount;
        writer.ResetWrittenCount();
        writer.Write(values);
        writer.Advance(Math.Max(0, count - values.Length));
        return writer;
    }
}

[SkipCategory]
public static class ReadOnlyMemoryExtensions
{
    /// <inheritdoc cref="MemoryMarshal.AsMemory{T}(ReadOnlyMemory{T})"/>
    public static Memory<T> AsMemory<T>(this ReadOnlyMemory<T> memory) => MemoryMarshal.AsMemory(memory);
}
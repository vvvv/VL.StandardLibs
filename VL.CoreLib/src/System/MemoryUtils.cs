using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VL.Core;

namespace VL.Lib
{
    public static class MemoryUtils
    {
        public static ReadOnlyMemory<T> AsReadOnlyMemory<T>(T[] input) => input;

        public static ReadOnlyMemory<T> AsReadOnlyMemory<T>(ImmutableArray<T> input) => input.AsMemory();

        public static ReadOnlyMemory<T> AsReadOnlyMemory<T>(IReadOnlyList<T> input)
        {
            if (input is null)
                return ReadOnlyMemory<T>.Empty;

            if (!input.TryGetMemory(out var memory))
                throw new ArgumentException($"Can't retrieve read-only memory from {input.GetType()}");

            return memory;
        }

        public static ReadOnlyMemory<char> AsReadOnlyMemory(string input) => input.AsMemory();

        public static Memory<T> AsMemory<T>(T[] input) => input;

        public static Memory<T> AsMemory<T>(IHasMemory<T> input)
        {
            if (input.TryGetMemory(out Memory<T> memory))
                return memory;
            return Memory<T>.Empty;
        }

        public static int SizeInBytes<T>(ReadOnlyMemory<T> input) => input.Length * Unsafe.SizeOf<T>();

        public static int ElementSizeInBytes<T>(ReadOnlyMemory<T> input) => Unsafe.SizeOf<T>();

        public static ReadOnlyMemory<TTo> Reinterpret<TFrom, TTo>(ReadOnlyMemory<TFrom> input)
            where TFrom : struct
            where TTo : struct
        {
            return Reinterpret<TFrom, TTo>(MemoryMarshal.AsMemory(input));
        }

        public static Memory<TTo> Reinterpret<TFrom, TTo>(Memory<TFrom> input)
            where TFrom : struct
            where TTo : struct
        {
            return new CastMemoryManager<TFrom, TTo>(input).Memory;
        }

        private sealed class CastMemoryManager<TFrom, TTo> : MemoryManager<TTo>
            where TFrom : struct
            where TTo : struct
        {
            private readonly Memory<TFrom> _from;

            public CastMemoryManager(Memory<TFrom> from) => _from = from;

            public override Span<TTo> GetSpan()
                => MemoryMarshal.Cast<TFrom, TTo>(_from.Span);

            protected override void Dispose(bool disposing) { }
            public override MemoryHandle Pin(int elementIndex = 0)
                => throw new NotSupportedException();
            public override void Unpin()
                => throw new NotSupportedException();
        }
    }
}

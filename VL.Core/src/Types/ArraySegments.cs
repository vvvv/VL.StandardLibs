using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace VL.Lib.Collections
{
    public class ImmutableArraySegment<T> : IReadOnlyList<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            private readonly ImmutableArray<T> _array;
            private readonly int _count;
            private readonly int _offset;
            private int _index;

            public Enumerator(ImmutableArraySegment<T> array)
            {
                _array = array._array;
                _count = array._count;
                _offset = array._offset;
                _index = 0;
                Current = default(T);
            }

            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                var index = _index++;
                if (index < _count)
                {
                    Current = _array[_offset + index];
                    return true;
                }
                else
                {
                    Current = default(T);
                    return false;
                }
            }

            public void Dispose() { }
            public void Reset() { }
        }

        class _Enumerator : IEnumerator<T>
        {
            private readonly ImmutableArray<T> _array;
            private readonly int _count;
            private readonly int _offset;
            private int _index;

            public _Enumerator(ImmutableArraySegment<T> list)
            {
                _array = list._array;
                _count = list._count;
                _offset = list._offset;
                _index = 0;
                Current = default(T);
            }

            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                var index = _index++;
                if (index < _count)
                {
                    Current = _array[_offset + index];
                    return true;
                }
                else
                {
                    Current = default(T);
                    return false;
                }
            }

            public void Dispose() { }
            public void Reset() { }
        }

        private readonly ImmutableArray<T> _array;
        private readonly int _offset;
        private readonly int _count;

        public ImmutableArraySegment(ImmutableArray<T> array, int offset, int count)
        {
            _array = array;
            _offset = offset;
            _count = count;
        }

        public T this[int index] => _array[_offset + index];
        public int Count => _count;
        public int Offset => _offset;

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new _Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new _Enumerator(this);
    }

    public class Segment<T> : IReadOnlyList<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _array;
            private readonly int _count;
            private readonly int _offset;
            private int _index;

            public Enumerator(Segment<T> segment)
            {
                _array = segment._array;
                _count = segment._count;
                _offset = segment._offset;
                _index = 0;
                Current = default(T);
            }

            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                var index = _index++;
                if (index < _count)
                {
                    Current = _array[_offset + index];
                    return true;
                }
                else
                {
                    Current = default(T);
                    return false;
                }
            }

            public void Dispose() { }
            public void Reset() { }
        }

        class _Enumerator : IEnumerator<T>
        {
            private readonly T[] _array;
            private readonly int _count;
            private readonly int _offset;
            private int _index;

            public _Enumerator(Segment<T> segment)
            {
                _array = segment._array;
                _count = segment._count;
                _offset = segment._offset;
                _index = 0;
                Current = default(T);
            }

            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                var index = _index++;
                if (index < _count)
                {
                    Current = _array[_offset + index];
                    return true;
                }
                else
                {
                    Current = default(T);
                    return false;
                }
            }

            public void Dispose() { }
            public void Reset() { }
        }

        private readonly T[] _array;
        private readonly int _offset;
        private readonly int _count;

        public Segment(T[] array, int offset, int count)
        {
            _array = array;
            _offset = offset;
            _count = count;
        }

        public T this[int index] => _array[_offset + index];
        public int Count => _count;
        public int Offset => _offset;

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new _Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new _Enumerator(this);
    }


    public static class ArraySegments
    {
        public static ImmutableArraySegment<T> GetSegment<T>(this ImmutableArray<T> array, int offset) => array.GetSegment(offset, array.Length - offset);

        public static ImmutableArraySegment<T> GetSegment<T>(this ImmutableArray<T> array, int offset, int count)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            var lc = array.Length;
            if ((offset + count) > lc)
                throw new IndexOutOfRangeException();
            return new ImmutableArraySegment<T>(array, offset, count);
        }

        public static Segment<T> GetSegment<T>(this T[] array, int offset) => array.GetSegment(offset, array.Length - offset);

        public static Segment<T> GetSegment<T>(this T[] array, int offset, int count)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            var lc = array.Length;
            if ((offset + count) > lc)
                throw new IndexOutOfRangeException();
            return new Segment<T>(array, offset, count);
        }
    }
}

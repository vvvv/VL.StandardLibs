using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using VL.Core;

namespace VL.Lib.Collections
{
    /// <summary>
    /// Allows to build up arrays in an efficient way.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    [Serializable]
    public class ArrayBuilder<T> : IReadOnlyList<T>, IHasMemory<T>
    {
        private const int DefaultCapacity = 4;

        T[] _array;
        int _count;

        public ArrayBuilder(int capacity)
        {
            _array = capacity > 0 ? new T[capacity] : Array.Empty<T>();
            _count = 0;
        }

        /// <summary>
        /// Gets the element at the specified index in the builder.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index in the builder.</returns>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ref AsSpan()[index];
            }
        }

        T IReadOnlyList<T>.this[int index] => this[index];

        /// <summary>
        /// Gets the element at the specified index in the builder or the default value if the index is out of range.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index in the builder or the default value if the index is out of range.</returns>
        public T ElementAtOrDefault(int index)
        {
            if (index >= 0 && index < _array.Length && index < _count)
                return _array[index];
            else
                return default;
        }

        /// <summary>
        /// The amount of items currently stored in the builder.
        /// </summary>
        public int Count
        {
            get => _count;
            set
            {
                if (value > _count)
                {
                    Array.Resize(ref _array, value);
                    _count = value;
                }
            }
        }

        /// <summary>
        /// The capacity of the builder.
        /// </summary>
        public int Capacity => _array.Length;

        /// <summary>
        /// Gives access to the internally used array. The length of the returned array might be greater then the item count of the builder.
        /// </summary>
        public T[] Buffer => _array;

        /// <summary>
        /// Exposes the content of the builder as a <see cref="Span{T}"/>.
        /// </summary>
        public Span<T> AsSpan() => _array.AsSpan(0, _count);

        /// <summary>
        /// Exposes the content of the builder as <see cref="Memory{T}"/>.
        /// </summary>
        public Memory<T> AsMemory() => _array.AsMemory(0, _count);

        /// <summary>
        /// Exposes the content of the builder as <see cref="ArraySegment{T}"/>.
        /// </summary>
        public ArraySegment<T> AsArraySegment() => new ArraySegment<T>(_array, 0, _count);

        /// <summary>
        /// Resizes the internal array to the count of the builder and returns it.
        /// </summary>
        public T[] AsArray()
        {
            if (_count != Capacity)
            {
                Array.Resize(ref _array, _count);
            }

            return _array;
        }

        /// <summary>
        /// Returns the upstream sequence as an array. This can be done efficiently without copying for immutable collections based on arrays like
        /// <see cref="Spread{T}"/> or <see cref="ImmutableArray{T}"/>. For other collection types the content will be copied into
        /// the internal array and returned.
        /// </summary>
        /// <param name="items">The upstream sequence.</param>
        /// <returns>The array from the upstream immutable array based collection or the internal array containing the content.</returns>
        public T[] AsArray(IEnumerable<T> items)
        {
            if (items.TryGetArray(out var array))
                return array;

            Clear();
            AddRange(items);
            return AsArray();
        }

        /// <summary>
        /// Add an item to the builder.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            if (_count == Capacity)
            {
                Array.Resize(ref _array, Math.Max(Capacity * 2, DefaultCapacity));
            }

            _array[_count++] = item;
        }

        /// <summary>
        /// Add an item to the builder without checking whether the capacity is large enough. Use only if capacity is large enough.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void UncheckedAdd(T item)
        {
            _array[_count++] = item;
        }

        /// <summary>
        /// Adds a range of items to the builder. The sequence will be checked for well known collection types in order to do the copying efficiently.
        /// </summary>
        /// <param name="items">The items to add.</param>
        public void AddRange(IEnumerable<T> items)
        {
            if (items.TryGetSpan(out var span))
            {
                AddRange(span);
            }
            else if (items is ICollection<T> collection)
            {
                AddRange(collection);
            }
            else
            {
                foreach (var x in items)
                    Add(x);
            }
        }

        /// <summary>
        /// Adds the items to the builder.
        /// </summary>
        /// <param name="items">The items to add.</param>
        public void AddRange(in ReadOnlySpan<T> items)
        {
            if (items.Length == 0)
                return;

            var newCount = _count + items.Length;
            if (newCount > Capacity)
            {
                Array.Resize(ref _array, newCount);
            }

            items.CopyTo(_array.AsSpan(_count, items.Length));

            _count = newCount;
        }

        /// <summary>
        /// Adds the items to the builder.
        /// </summary>
        /// <param name="items">The items to add.</param>
        public void AddRange(T[] items) => AddRange(items.AsSpan());

        /// <summary>
        /// Adds the items to the builder.
        /// </summary>
        /// <param name="items">The items to add.</param>
        public void AddRange(ICollection<T> items)
        {
            if (items.Count == 0)
                return;

            var newCount = _count + items.Count;
            if (newCount > Capacity)
            {
                Array.Resize(ref _array, newCount);
            }

            items.CopyTo(_array, _count);

            _count = newCount;
        }

        /// <summary>
        /// Clears the builder.
        /// </summary>
        public void Clear()
        {
            if (_count > 0 && !typeof(T).IsValueType)
            {
                Array.Clear(_array, 0, _count);
            }

            _count = 0;
        }

        /// <summary>
        /// Copies the current contents to the specified array.
        /// </summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="index">The starting index of the target array.</param>
        public void CopyTo(T[] array, int index)
        {
            var src = AsSpan();
            var dst = array.AsSpan().Slice(index);
            src.CopyTo(dst);
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="IList{T}"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="IList{T}"/>.</param>
        /// <returns>
        /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(T item) => Array.IndexOf(_array, item, 0, _count);

        /// <summary>
        /// Inserts an item to the <see cref="IList{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="IList{T}"/>.</param>
        public void Insert(int index, T item)
        {
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (_count == Capacity)
            {
                Array.Resize(ref _array, Math.Max(Capacity * 2, DefaultCapacity));
            }

            if (index < _count)
            {
                Array.Copy(_array, index, _array, index + 1, _count - index);
            }

            _count++;
            _array[index] = item;
        }

        /// <summary>
        /// Removes the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>A value indicating whether the specified element was found and removed from the collection.</returns>
        public bool Remove(T element)
        {
            var index = IndexOf(element);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the <see cref="IList{T}"/> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (index < _count - 1)
            {
                Array.Copy(_array, index + 1, _array, index, _count - index - 1);
            }

            _count--;
        }

        /// <summary>
        /// Reverses the order of elements in the collection.
        /// </summary>
        public void Reverse()
        {
            Array.Reverse(_array, 0, _count);
        }

        /// <summary>
        /// Sorts the array.
        /// </summary>
        public void Sort()
        {
            if (_count > 1)
            {
                Array.Sort(_array, 0, _count, Comparer<T>.Default);
            }
        }

        /// <summary>
        /// Sorts the array.
        /// </summary>
        /// <param name="comparer">The comparer to use in sorting. If <c>null</c>, the default comparer is used.</param>
        public void Sort(IComparer<T> comparer)
        {
            if (_count > 1)
            {
                Array.Sort(_array, 0, _count, comparer);
            }
        }

        /// <summary>
        /// Sorts the array.
        /// </summary>
        /// <param name="comparer">The comparer to use in sorting.</param>
        public void Sort(Comparison<T> comparer) => Sort(new Comparer(comparer));

        /// <summary>
        /// Sorts the array.
        /// </summary>
        /// <param name="index">The index of the first element to consider in the sort.</param>
        /// <param name="count">The number of elements to include in the sort.</param>
        /// <param name="comparer">The comparer to use in sorting. If <c>null</c>, the default comparer is used.</param>
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || index + count > _count)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count > 1)
            {
                Array.Sort(_array, index, count, comparer);
            }
        }

        public Span<T>.Enumerator GetEnumerator()
        {
            return AsSpan().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return MemoryMarshal.ToEnumerable<T>(AsMemory()).GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return MemoryMarshal.ToEnumerable<T>(AsMemory()).GetEnumerator();
        }

        public bool TryGetMemory(out Memory<T> memory)
        {
            memory = AsMemory();
            return true;
        }

        public bool TryGetMemory(out ReadOnlyMemory<T> memory)
        {
            memory = AsMemory();
            return true;
        }

        class Comparer : IComparer<T>
        {
            private readonly Comparison<T> comparison;

            public Comparer(Comparison<T> comparison)
            {
                this.comparison = comparison;
            }

            public int Compare(T x, T y) => comparison(x, y);
        }
    }
}

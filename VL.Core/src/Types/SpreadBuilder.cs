using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using VL.Core;

namespace VL.Lib.Collections
{
    /// <summary>
    /// Non-generic view on spread builder.
    /// </summary>
    public interface ISpreadBuilder : IList
    {
        /// <summary>
        /// Returns an immutable spread of the current contents of the builder.
        /// </summary>
        ISpread ToSpread();
    }

    public static class SpreadUtils
    {
        public static Spread<T> SelectFromUngeneric<T>(this IList ungenericSpread, Func<object, T> selector)
        {
            var builder = new SpreadBuilder<T>(ungenericSpread.Count);
            for (int i = 0; i < ungenericSpread.Count; i++)
            {
                builder.Add(selector(ungenericSpread[i]));
            }
            return builder.ToSpread();
        }

        public static ISpread SelectFromUngeneric(this IList ungenericSpread, Func<object, object> selector, Type elementType)
        {
            var builder = Activator.CreateInstance(typeof(SpreadBuilder<>).MakeGenericType(elementType)) as ISpreadBuilder;
            for (int i = 0; i < ungenericSpread.Count; i++)
            {
                builder.Add(selector(ungenericSpread[i]));
            }
            return builder.ToSpread();
        }

        public static Spread<T> SelectFromUngeneric<T>(this ICollection ungenericSpread, Func<object, T> selector)
        {
            var builder = new SpreadBuilder<T>(ungenericSpread.Count);
            foreach (var item in ungenericSpread)
            {
                builder.Add(selector(item));
            }
            return builder.ToSpread();
        }

        public static ISpread SelectFromUngeneric(this ICollection ungenericSpread, Func<object, object> selector, Type elementType)
        {
            var builder = Activator.CreateInstance(typeof(SpreadBuilder<>).MakeGenericType(elementType)) as ISpreadBuilder;
            foreach (var item in ungenericSpread)
            {
                builder.Add(selector(item));
            }
            return builder.ToSpread();
        }
    }


    [Serializable]
    public class SpreadBuilder<T> : IList<T>, IReadOnlyList<T>, IHasMemory<T>, ISpreadBuilder
    {
        public static SpreadBuilder<T> Empty => new SpreadBuilder<T>();

        static readonly EqualityComparer<T> _comparer = EqualityComparer<T>.Default;

        private ImmutableArray<T>.Builder _builder;
        private Spread<T> _spread;

        public SpreadBuilder()
        {
            _builder = ImmutableArray.CreateBuilder<T>();
        }

        public SpreadBuilder(int capacity)
        {
            _builder = ImmutableArray.CreateBuilder<T>(Math.Max(capacity, 0));
        }

        public SpreadBuilder(Spread<T> spread)
        {
            _spread = spread ?? throw new ArgumentNullException(nameof(spread));
        }

        public SpreadBuilder(IEnumerable<T> values) 
            : this()
        {
            if (values != null)
            {               
                _builder.AddRange(values);
            }
        }

        public SpreadBuilder(T[] array)
        {
            _builder = ImmutableArray.CreateBuilder<T>(array.Length);
            _builder.AddRange(array);
        }

        public Spread<T> ToSpread()
        {
            if (_spread != null)
                return _spread;

            if (_builder.Count == 0)
                return Spread<T>.Empty;
            
            if (_builder.Count == _builder.Capacity)
            {
                // Transfer ownership to the spread.
                var builder = _builder;
                _builder = null;
                return _spread = new Spread<T>(builder.MoveToImmutable());
            }

            return new Spread<T>(_builder.ToImmutable());
        }

        private void CopyOnWrite()
        {
            if (_builder == null)
            {
                _builder = _spread._array.ToBuilder();
                _spread = null;
            }
        }

        /// <summary>
        /// Searches for the specified item and returns index of the first occurrence
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            if (_builder != null)
                return _builder.IndexOf(item);
            return _spread.IndexOf(item);
        }

        /// <summary>
        /// Inserts the item at specified index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, T item)
        {
            CopyOnWrite();
            _builder.Insert(index, item);
        }

        /// <summary>
        /// Removes the item at specified index
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            CopyOnWrite();
            _builder.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                if (_builder != null)
                    return _builder[index];
                return _spread[index];
            }
            set
            {
                if (_builder != null)
                {
                    _builder[index] = value;
                }
                // Avoid memory allocations
                else if (!_comparer.Equals(_spread[index], value))
                {
                    CopyOnWrite();
                    _builder[index] = value;
                }
            }
        }

        public void Add(T item)
        {
            CopyOnWrite();
            _builder.Add(item);
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items is Spread<T> spread)
                AddRange(spread);
            else if (items is SpreadBuilder<T> builder)
                AddRange(builder);
            else
            {
                var collection = items as IReadOnlyCollection<T>;
                if (collection == null || collection.Count > 0)
                {
                    CopyOnWrite();
                    // Already checks for List, Array, ImmutableArray and ImmutableArray.Builder
                    _builder.AddRange(items);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(Spread<T> items) => AddRange(items._array);

        public void AddRange(T[] items)
        {
            if (items.Length > 0)
            {
                CopyOnWrite();
                _builder.AddRange(items);
            }
        }

        public void AddRange(SpreadBuilder<T> items)
        {
            var arrayBuilder = items._builder;
            if (arrayBuilder != null)
                AddRange(arrayBuilder);
            else
                AddRange(items._spread);
        }

        public void AddRange(ImmutableArray<T> items)
        {
            if (items.Length > 0)
            {
                CopyOnWrite();
                _builder.AddRange(items);
            }
        }

        public void AddRange(ImmutableArray<T>.Builder items)
        {
            if (items.Count > 0)
            {
                CopyOnWrite();
                _builder.AddRange(items);
            }
        }

        public void AddRangeArray(T[] array, int count, int arrayIndex, bool reverseOrder)
        {
            CopyOnWrite();
            if (reverseOrder)
            {
                var startIndex =  count - 1;
                for (int i = startIndex; i >= 0; i--)
                {
                    _builder.Add(array[arrayIndex + i]);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    _builder.Add(array[arrayIndex + i]);
                }
            }
        }

        public void AddRange(T[] items, int length)
        {
            CopyOnWrite();
            _builder.AddRange(items, length);
        }

        public void SetRange(IEnumerable<T> items, int index)
        {
            foreach(var item in items)
            {
                this[index++] = item;
            }
        }

        public void SetRangeArray(T[] array, int startIndex, int count, int arrayIndex, bool reverseOrder)
        {
            if (reverseOrder)
            {
                startIndex = startIndex + count - 1;
                for (int i = 0; i < count; i++)
                {
                    this[startIndex - i] = array[arrayIndex + i];
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    this[startIndex + i] = array[arrayIndex + i];
                } 
            }
        }

        public void Clear()
        {
            CopyOnWrite();
            _builder.Clear();
        }

        public bool Contains(T item)
        {
            if (_builder != null)
                return _builder.Contains(item);
            return ((IReadOnlyCollection<T>)_spread).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (_builder != null)
                _builder.CopyTo(array, arrayIndex);
            else
                ((ICollection)_spread).CopyTo(array, arrayIndex);
        }

        public T[] CopyToVL(T[] array, int arrayIndex)
        {
            if (_builder != null)
                _builder.CopyTo(array, arrayIndex);
            else
                ((ICollection)_spread).CopyTo(array, arrayIndex);

            return array;
        }

        /// <summary>
        /// Copies 'Count' elements from 'Start Index' to an Array at the 'Array Index' position
        /// </summary>
        /// <param name="array"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="arrayIndex"></param>
        /// <param name="reverseOrder"></param>
        /// <returns></returns>
        public T[] CopyTo(T[] array, int startIndex, int count, int arrayIndex, bool reverseOrder)
        {
            if (reverseOrder)
            {
                startIndex = startIndex + count - 1;
                for (int i = 0; i < count; i++)
                {
                    array[arrayIndex + i] = this[startIndex - i];
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    array[arrayIndex + i] = this[startIndex + i];
                }
            }

            return array;
        }

        public int Count
        {
            get
            { 
                if (_builder != null)
                    return _builder.Count;
                return _spread.Count;
            }
            set
            {
                if (value != Count)
                {
                    CopyOnWrite();
                    _builder.Count = value;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool IsFixedSize => false;

        public object SyncRoot => this;

        public bool IsSynchronized => false;

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }

            set
            {
                this[index] = (T)value;
            }
        }

        public bool Remove(T item)
        {
            CopyOnWrite();
            return _builder.Remove(item);
        }

        public void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                CopyOnWrite();
                for (int i = 0; i < count; i++)
                    _builder.RemoveAt(index);
            }
        }

        /// <summary>
        /// Removes all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public void RemoveAll(Func<T, bool> match)
        {
            CopyOnWrite();
            _builder.RemoveAll(x => match(x));
        }

        /// <summary>
        /// Sorts the elements in the entire spread builder using the default comparer
        /// </summary>
        public void Sort()
        {
            if (Count <= 1)
                return;
            CopyOnWrite();
            _builder.Sort();
        }

        /// <summary>
        /// Sorts the elements in the entire spread builder using the specified comparer function
        /// </summary>
        /// <param name="comparer"></param>
        public void Sort(Func<T, T, int> comparer)
        {
            if (Count <= 1)
                return;
            CopyOnWrite();
            _builder.Sort((a, b) => comparer(a, b));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T>.Enumerator GetEnumerator()
        {
            ReadOnlySpan<T> span;
            if (_builder != null)
                span = _builder.AsSpan();
            else
                span = _spread.AsSpan();
            return span.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> GetObjectEnumerator()
        {
            if (_builder != null)
                return _builder.GetEnumerator();
            return ((IEnumerable<T>)_spread).GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetObjectEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetObjectEnumerator();
        }

        ISpread ISpreadBuilder.ToSpread() => ToSpread();

        public int Add(object value)
        {
            Add((T)value);
            return Count - 1;
        }

        public bool Contains(object value) => Contains((T)value);
        public int IndexOf(object value) => IndexOf((T)value);
        public void Insert(int index, object value) => Insert(index, (T)value);
        public void Remove(object value) => Remove((T)value);
        public void CopyTo(Array array, int index) => CopyTo((T[])array, index);

        #region IHasMemory

        public bool TryGetMemory(out Memory<T> memory)
        {
            CopyOnWrite();
            var elements = _builder.GetInternalArray();
            memory = new Memory<T>(elements, 0, _builder.Count);
            return true;
        }

        public bool TryGetMemory(out ReadOnlyMemory<T> memory)
        {
            if (_builder != null)
            {
                var elements = _builder.GetInternalArray();
                memory = new ReadOnlyMemory<T>(elements, 0, _builder.Count);
                return true;
            }
            return _spread.TryGetMemory(out memory);
        }

        #endregion
    }
}
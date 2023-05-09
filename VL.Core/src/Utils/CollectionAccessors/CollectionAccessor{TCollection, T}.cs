using System;
using System.Collections;
using System.Collections.Generic;

namespace VL.Core.Utils
{
    abstract class CollectionAccessor<TCollection, TElement> : ICollectionAccessor<TElement>
        where TCollection : ICollection<TElement>
    {
        private TCollection collection;

        protected TCollection Collection
        {
            get => collection;
            set
            {
                collection = value;
                CollectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public object UnderlyingCollection
        {
            get => Collection;
            set => Collection = (TCollection)value;
        }

        public event EventHandler CollectionChanged;

        public int Count => Collection.Count;

        public bool IsSynchronized => Collection is IList list ? list.IsSynchronized : false;

        public object SyncRoot => Collection is IList list ? list.SyncRoot : null;

        public void CopyTo(Array array, int index)
        {
            Collection.CopyTo((TElement[])array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        public bool IsReadOnly => Collection.IsReadOnly;

        bool IList.IsFixedSize => Collection is IList list ? list.IsFixedSize : true;

        object? IList.this[int index] { get => this[index]; set => this[index] = (TElement)value!; }

        public abstract TElement this[int index] { get; set; }

        public virtual void Add(TElement item)
        {
            Collection.Add(item);
        }

        public virtual void Clear()
        {
            Collection.Clear();
        }

        public bool Contains(TElement item)
        {
            return Collection.Contains(item);
        }

        public void CopyTo(TElement[] array, int arrayIndex)
        {
            Collection.CopyTo(array, arrayIndex);
        }

        public virtual bool Remove(TElement item)
        {
            return Collection.Remove(item);
        }

        IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        public abstract int IndexOf(TElement item);
        public abstract void Insert(int index, TElement item);
        public abstract void RemoveAt(int index);

        int IList.Add(object? value)
        {
            Add((TElement)value!);
            return Count - 1;
        }

        bool IList.Contains(object? value) => Contains((TElement)value!);

        int IList.IndexOf(object? value) => IndexOf((TElement)value!);

        void IList.Insert(int index, object? value) => Insert(index, (TElement)value!);

        void IList.Remove(object? value) => Remove((TElement)value!);
    }
}

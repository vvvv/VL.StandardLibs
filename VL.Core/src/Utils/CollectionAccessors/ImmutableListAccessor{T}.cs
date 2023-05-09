using System.Collections;
using System.Collections.Immutable;

namespace VL.Core.Utils
{
    sealed class ImmutableListAccessor<T> : ListAccessor<ImmutableList<T>, T>
    {
        public override T this[int index]
        {
            get => Collection[index];
            set => UnderlyingCollection = Collection.SetItem(index, value);
        }

        public override void Add(T item)
        {
            UnderlyingCollection = Collection.Add(item);
        }

        public override void Clear()
        {
            UnderlyingCollection = Collection.Clear();
        }

        public override void Insert(int index, T item)
        {
            UnderlyingCollection = Collection.Insert(index, item);
        }

        public override bool Remove(T item)
        {
            var collection = Collection.Remove(item);
            if (collection != Collection)
            {
                UnderlyingCollection = collection;
                return true;
            }
            return false;
        }

        public override void RemoveAt(int index)
        {
            UnderlyingCollection = Collection.RemoveAt(index);
        }
    }
}

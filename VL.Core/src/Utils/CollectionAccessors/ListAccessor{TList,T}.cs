using System.Collections;
using System.Collections.Generic;

namespace VL.Core.Utils
{
    abstract class ListAccessor<TList, T> : CollectionAccessor<TList, T>, IList<T>
        where TList : IList<T>
    {
        public override T this[int index]
        {
            get => Collection[index];
            set => Collection[index] = value;
        }

        public override sealed int IndexOf(T item)
        {
            return Collection.IndexOf(item);
        }

        public override void Insert(int index, T item)
        {
            Collection.Insert(index, item);
        }

        public override void RemoveAt(int index)
        {
            Collection.RemoveAt(index);
        }
    }
}

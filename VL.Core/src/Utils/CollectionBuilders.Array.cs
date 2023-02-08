using System;

namespace VL.Core.Utils
{
    partial class CollectionBuilders
    {
        public struct ArrayBuilder<T>
        {
            T[] array;
            int index;

            public ArrayBuilder(T[] array, int capacity)
            {
                this.array = array ?? new T[capacity];
                index = 0;
            }

            public void Add(T value)
            {
                var i = index++;
                if (i == array.Length)
                    Array.Resize(ref array, Math.Max(1, array.Length * 2));
                array[i] = value;
            }

            public T[] Commit()
            {
                if (index == array.Length)
                    return array;
                if (index == 0)
                    return Array.Empty<T>();
                Array.Resize(ref array, index);
                return array;
            }
        }
    }
}

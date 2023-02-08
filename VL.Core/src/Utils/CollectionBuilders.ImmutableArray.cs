using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace VL.Core.Utils
{
    partial class CollectionBuilders
    {
        public struct ImmutableArrayBuilder<T>
        {
            static readonly EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            ImmutableArray<T>.Builder builder;
            ImmutableArray<T> array;
            int index;

            public ImmutableArrayBuilder(ImmutableArray<T> array, int capacity)
            {
                this.builder = array.IsDefault ? ImmutableArray.CreateBuilder<T>(capacity) : null;
                this.array = array;
                this.index = 0;
            }

            public void Add(T value)
            {
                if (builder != null)
                    builder.Add(value);
                else
                {
                    // Check if value is the same
                    var i = index++;
                    var array = this.array;
                    if (i < array.Length && comparer.Equals(array[i], value))
                        return;

                    // Copy existing and add value
                    builder = ImmutableArray.CreateBuilder<T>(Math.Max(array.Length, 1));
                    builder.AddRange(array, i);
                    builder.Add(value);
                }
            }

            public ImmutableArray<T> Commit()
            {
                if (builder != null)
                {
                    if (builder.Count == 0)
                        return ImmutableArray<T>.Empty;

                    if (builder.Count == builder.Capacity)
                    {
                        // Transfer ownership.
                        return builder.MoveToImmutable();
                    }

                    return builder.ToImmutable();
                }

                if (index != array.Length)
                {
                    var b = ImmutableArray.CreateBuilder<T>(index);
                    b.AddRange(array, index);
                    return b.MoveToImmutable();
                }

                return array;
            }
        }
    }
}

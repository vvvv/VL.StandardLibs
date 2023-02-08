using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using VL.Lib.Collections;

namespace VL.Core.Utils
{
    partial class CollectionBuilders
    {
        public struct SpreadBuilder<T>
        {
            ImmutableArrayBuilder<T> builder;
            Spread<T> spread;

            public SpreadBuilder(Spread<T> spread, int capacity)
            {
                var array = spread != null ? spread._array : default;
                this.builder = new ImmutableArrayBuilder<T>(array, capacity);
                this.spread = spread;
            }

            public SpreadBuilder(Spread<T> spread)
            {
                var array = spread != null ? spread._array : default;
                this.builder = new ImmutableArrayBuilder<T>(array, array.Length);
                this.spread = spread;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(T value) => builder.Add(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddRange(IEnumerable<T> items)
            {
                // can we impove this further?
                foreach (var item in items)
                    builder.Add(item);
            }

            public Spread<T> Commit()
            {
                var array = builder.Commit();
                if (array.Length == 0)
                    return Spread<T>.Empty;
                if (spread != null && spread._array == array)
                    return spread;
                return new Spread<T>(array);
            }
        }
    }
}

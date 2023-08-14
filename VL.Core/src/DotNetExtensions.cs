#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VL.Core;
using VL.Lib.Collections;

namespace System.Collections.Generic
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> YieldReturn<T>(this T item)
        {
            yield return item;
        }

        public static IEnumerable<T> SaveEnumerable<T>(this IEnumerable<T> items)
        {
            return items ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<T> YieldReturnIfNotNull<T>(this T item)
        {
            if (item != null)
                yield return item;
        }

        public static IEnumerable<T> YieldIf<T>(this T item, bool @yield)
        {
            if (@yield)
                yield return item;
        }

        public static IEnumerable<T> YieldReturnMany<T>(this T item, int count)
        {
            for (int i = 0; i < count; i++)
                yield return item;
        }

        public static IEnumerable<TOut> SelectIf<T, TOut>(this IEnumerable<T> input, Func<T, (bool, TOut)> selector)
        {
            foreach (var i in input)
            {
                var _ = selector(i);
                if (_.Item1)
                    yield return _.Item2;
            }
        }

        public delegate (bool result, TValue value) TryAndSelect<in TKey, TValue>(TKey key);

        public static IEnumerable<TOut> TrySelect<T, TOut>(this IEnumerable<T> input, TryAndSelect<T, TOut> selector)
        {
            foreach (var i in input)
            {
                var (result, v) = selector(i);
                if (result)
                    yield return v;
            }
        }

        public static IEnumerable<T> ConcatIfNotNull<T>(this T input, T other)
        {
            yield return input;
            if (other != null)
                yield return other;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool None<T>(this ImmutableArray<T> collection) => collection.Length == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool One<T>(this ImmutableArray<T> collection) => collection.Length == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Many<T>(this ImmutableArray<T> collection) => collection.Length > 1;

        public static bool Many<T>(this IEnumerable<T> collection)
        {
            //avoiding Count(), which would enumerate everything...
            //might still be optimizable with own enumerator.
            return collection.Skip(1).Any();
        }

        public static bool Many<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            //avoiding Count(), which would enumerate everything...
            //might still be optimizable with own enumerator.
            return collection.Where(predicate).Skip(1).Any();
        }

        public static bool None<T>(this IEnumerable<T> collection)
        {
            return !collection.Any();
        }

        public static bool None<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            return !collection.Any(predicate);
        }

        public static bool One<T>(this IEnumerable<T> collection)
        {
            //avoiding Count(), which would enumerate everything...
            var count = 0;
            foreach (var item in collection)
            {
                if (++count > 1)
                    return false;
            }
            return count == 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? SingleOrDefaultIfNoneOrMany<T>(this ImmutableArray<T> collection)
        {
            if (collection.Length == 1)
                return collection[0];
            return default(T);
        }

        public static T? SingleOrDefaultIfNoneOrMany<T>(this IEnumerable<T> collection)
        {
            var enumerator = collection.GetEnumerator();
            if (!enumerator.MoveNext()) return default(T); //one movenext shall return true
            var item = enumerator.Current;
            if (enumerator.MoveNext()) return default(T); //next movenext shall return false
            
            return item;
        }
        
        public static IEnumerable<T> SaveConcat<T>(this IEnumerable<T> a, IEnumerable<T> b)
        {
            if (a == null)
                return b;
            else
                if (b == null)
                return a;
            else
                return a.Concat(b);
        }        
        
        /// <summary>
        /// (a,b),(c,d) -> (a,c),(a,d),(b,c),(b,d)
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Permutations<T>(this IEnumerable<IEnumerable<T>> sources)
        {
            if (sources.Many())
            {
                var first = sources.First();        		
                var othersPermuatations = sources.Skip(1).Permutations();
                foreach (var x in first)
                {
                    foreach (var ys in othersPermuatations)
                        yield return x.YieldReturn().Concat(ys);
                }
            }
            else
            {        		
                if (sources.Any())
                {
                    var first = sources.First();        		
                    foreach (var x in first)
                        yield return x.YieldReturn();
                }
                else
                    yield break;
            }
        }

        public static void RemoveAll<T>(this ImmutableArray<T>.Builder builder, Predicate<T> predicate)
        {
            for (int i = builder.Count - 1; i >= 0; i--)
                if (predicate(builder[i]))
                    builder.RemoveAt(i);
        }

        public static void InsertRange<T>(this ImmutableArray<T>.Builder builder, int index, IEnumerable<T> values)
        {
            foreach (var value in values.Reverse())
                builder.Insert(index, value);
        }

        /// <summary>
        /// Tries to fetch the underlying array from the given sequence. Tests for array, <see cref="ImmutableArray{T}"/> and <see cref="Spread{T}"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="sequence">The sequence to test.</param>
        /// <param name="array">The underlying array.</param>
        /// <returns>True if the array could be fetched.</returns>
        public static bool TryGetArray<T>(this IEnumerable<T> sequence, out T[]? array)
        {
            if (sequence is T[] _array)
            {
                array = _array;
                return true;
            }
            else if (sequence is Spread<T> spread)
            {
                array = spread.GetInternalArray();
                return true;
            }
            else if (sequence is ImmutableArray<T> immutableArray)
            {
                array = Unsafe.As<ImmutableArray<T>, T[]>(ref immutableArray);
                return true;
            }
            else
            {
                array = default;
                return false;
            }
        }

        /// <summary>
        /// Tries to fetch the underlying array segment from the given sequence. Tests for array, <see cref="ImmutableArray{T}"/> and <see cref="IHasMemory{T}"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="sequence">The sequence to test.</param>
        /// <param name="segment">The underlying array segment.</param>
        /// <returns>True if the array segment could be fetched.</returns>
        public static bool TryGetArraySegment<T>(this IEnumerable<T> sequence, out ArraySegment<T> segment)
        {
            if (sequence is IHasMemory<T> hasMemory && hasMemory.TryGetMemory(out ReadOnlyMemory<T> memory) && MemoryMarshal.TryGetArray(memory, out segment))
            {
                return true;
            }
            else if (sequence is T[] _array)
            {
                segment = new ArraySegment<T>(_array);
                return true;
            }
            else if (sequence is ImmutableArray<T> immutableArray)
            {
                segment = new ArraySegment<T>(Unsafe.As<ImmutableArray<T>, T[]>(ref immutableArray));
                return true;
            }
            else
            {
                segment = default;
                return false;
            }
        }

        /// <summary>
        /// Tries to fetch the backing memory of the provided sequence. Tests for collections implementing <see cref="IHasMemory{T}"/>, 
        /// array and <see cref="ImmutableArray{T}"/>.
        /// </summary>
        /// <typeparam name="T">The elment type.</typeparam>
        /// <param name="sequence">The sequence to test.</param>
        /// <param name="memory">The content of the sequence as memory.</param>
        /// <returns>True if the memory could be fetched.</returns>
        public static bool TryGetMemory<T>(this IEnumerable<T> sequence, out ReadOnlyMemory<T> memory)
        {
            // All our collections support IHasMemory
            if (sequence is IHasMemory<T> hasMemory && hasMemory.TryGetMemory(out memory))
            {
                return true;
            }
            else if (sequence is T[] array)
            {
                memory = array.AsMemory();
                return true;
            }
            else if (sequence is ImmutableArray<T> immutableArray)
            {
                memory = immutableArray.AsMemory();
                return true;
            }
            else if (sequence is ImmutableArray<T>.Builder builder)
            {
                memory = builder.GetInternalArray().AsMemory(0, builder.Count);
                return true;
            }
            else if (sequence is List<T> list)
            {
                // TODO: In .NET5 we can use CollectionsMarshal for this
                memory = list.GetInternalArray().AsMemory(0, list.Count);
                return true;
            }
            else
            {
                memory = default;
                return false;
            }
        }

        /// <summary>
        /// Tries to expose the sequence as a <see cref="ReadOnlySpan{T}"/>. Tests for collections implementing <see cref="IHasMemory{T}"/>, 
        /// array and <see cref="ImmutableArray{T}"/>.
        /// </summary>
        /// <typeparam name="T">The elment type.</typeparam>
        /// <param name="sequence">The sequence to test.</param>
        /// <param name="span">The content of the sequence exposed as a span.</param>
        /// <returns>True if the content could be exposed as a span.</returns>
        public static bool TryGetSpan<T>(this IEnumerable<T> sequence, out ReadOnlySpan<T> span)
        {
            // All our collections support IHasMemory
            if (sequence is IHasMemory<T> hasMemory && hasMemory.TryGetMemory(out ReadOnlyMemory<T> memory))
            {
                span = memory.Span;
                return true;
            }
            else if (sequence is T[] array)
            {
                span = array.AsSpan();
                return true;
            }
            else if (sequence is ImmutableArray<T> immutableArray)
            {
                span = immutableArray.AsSpan();
                return true;
            }
            else if (sequence is ImmutableArray<T>.Builder builder)
            {
                span = builder.GetInternalArray().AsSpan(0, builder.Count);
                return true;
            }
            else
            {
                span = default;
                return false;
            }
        }
    }

    public static class DictionaryExtensions1
    {
        public static T? ValueOrNull<TKey, T>(this IReadOnlyDictionary<TKey, T> dict, TKey key)
            where T : class
        {
            if (dict.TryGetValue(key, out var value))
                return value;
            else
                return null;
        }

        public static IEnumerable<T> ValueOrNone<TKey, T>(this IReadOnlyDictionary<TKey, T> dict, TKey key)
        {
            if (dict.TryGetValue(key, out var value))
                yield return value;
        }

        public static TValue? ValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dict, TKey key, TValue? defaultValue = default(TValue))
        {
            if (dict.TryGetValue(key, out var value))
                return value;
            else
                return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue? ValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue? defaultValue = default(TValue))
            where TKey : notnull
        {
            if (dict.TryGetValue(key, out var value))
                return value;
            else
                return defaultValue;
        }

        /// <summary>
        /// create the value if not already stored for that key
        /// </summary>
        public static TValue EnsureValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> creator)
            where TKey : notnull
        {
            if (!dictionary.TryGetValue(key, out var value))
            {
                value = creator(key);
                dictionary[key] = value;
            }
            return value;
        }
    }

    public static class ListExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddIfNotNull<T>(this List<T> list, T item)
        {
            if (item != null)
                list.Add(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRangeIfNotNull<T>(this List<T> list, IEnumerable<T> items)
        {
            if (items != null)
                list.AddRange(items);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddOrThrowIfNull<T>(this List<T> list, T item)
        {
            if (item != null)
                list.Add(item);
            else
                throw new ArgumentNullException(nameof(item));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool None<T>(this List<T> list) => list.Count == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool One<T>(this List<T> list) => list.Count == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Many<T>(this List<T> list) => list.Count > 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            var tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }

        public static void RemoveRange<T>(this List<T> list, int start)
        {
            var count = list.Count - start;
            if (count > 0)
                list.RemoveRange(start, count);
        }

        /// <summary>
        /// throws if input is empty. so please check that first
        /// </summary>
        public static T ClampedElementAtIfNotEmpty<T>(this IList<T> input, ref int index)
        {
            index = index < 0 ? 0 : index > input.Count - 1 ? input.Count - 1 : index;
            return input[index];
        }

        /// <summary>
        /// if count = 0 returns a default(T)
        /// </summary>
        public static T? ClampedElementAtOrDefault<T>(this IList<T> input, ref int index)
        {
            if (input.Count == 0)
                return default(T);
            index = index < 0 ? 0 : index > input.Count - 1 ? input.Count - 1 : index;
            return input[index];
        }

        static class InternalArrayAccessor<T>
        {
            static Func<List<T>, T[]>? getInternalArray;

            public static T[] GetInternalArray(List<T> list)
            {
                if (list is null)
                    throw new ArgumentNullException(nameof(list));

                return (getInternalArray ??= Compile())(list);

                static Func<List<T>, T[]> Compile()
                {
                    const string itemsFieldName = "_items";
                    var type = typeof(List<T>);
                    var _itemsField = type.GetField(itemsFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (_itemsField == null)
                        throw new NotSupportedException($"Couldn't find field {itemsFieldName} in {type}");
                    var l = Expression.Parameter(type);
                    return Expression.Lambda<Func<List<T>, T[]>>(
                        body: Expression.MakeMemberAccess(l, _itemsField),
                        parameters: l).Compile();
                }
            }
        }

        // TODO: In .NET 5 we can use https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.collectionsmarshal.asspan?view=net-5.0
        public static T[] GetInternalArray<T>(this List<T> list)
        {
            return InternalArrayAccessor<T>.GetInternalArray(list);
        }
    }

    public static class HashSetExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRange<TEnumerable, TItem>(this HashSet<TItem> hashSet, TEnumerable values) where TEnumerable : IEnumerable<TItem>
        {
            foreach (var value in values)
                hashSet.Add(value);
        }
    }

    public static class StringExtensions
    {
        public static bool Contains(this string source, string value, StringComparison comparisonType)
        {
            return source != null && source.IndexOf(value, comparisonType) >= 0;
        }
    }

    public static class TimeSpanExtensions
    {
        public static string ToSecondsString(this TimeSpan interval)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:f1} s", interval.TotalSeconds);
        }

        public static string ToMilliSecondsString(this TimeSpan interval)
        {

            return string.Format(CultureInfo.InvariantCulture, "{0:f1} ms", interval.TotalMilliseconds);
        }

        public static string ToMicroSecondsString(this TimeSpan interval)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:f1} µs", interval.TotalMilliseconds * 1000);
        }
    }
}

namespace System.Reflection
{
    public static class ReflectionExtensions
    {
        public static T? GetCustomAttributeSafe<T>(this Assembly assembly) where T : Attribute
        {
            try
            {
                if (assembly == null) return default;
                return assembly.GetCustomAttribute<T>();
            }
            catch (FileNotFoundException)
            {
                return default;
            }
            catch (TypeLoadException)
            {
                // When trying to load Bridge.Html5
                return default;
            }
        }

        public static IEnumerable<T> GetCustomAttributesSafe<T>(this Assembly assembly) where T : Attribute
        {
            try
            {
                if (assembly == null) return Enumerable.Empty<T>();
                return assembly.GetCustomAttributes<T>();
            }
            catch (FileNotFoundException)
            {
                return Enumerable.Empty<T>();
            }
            catch (TypeLoadException)
            {
                // When trying to load Bridge.Html5
                return Enumerable.Empty<T>();
            }
        }

        public static T? GetCustomAttributeSafe<T>(this Type type) where T : Attribute
        {
            try
            {
                if (type == null) return default;
                return type.GetCustomAttribute<T>();
            }
            catch (FileNotFoundException)
            {
                return default;
            }
            catch (TypeLoadException)
            {
                // When trying to load Bridge.Html5
                return default;
            }
        }

        public static IEnumerable<T> GetCustomAttributesSafe<T>(this Type type) where T : Attribute
        {
            try
            {
                if (type == null) return Enumerable.Empty<T>();
                return type.GetCustomAttributes<T>();
            }
            catch (FileNotFoundException)
            {
                return Enumerable.Empty<T>();
            }
            catch (TypeLoadException)
            {
                // When trying to load Bridge.Html5
                return Enumerable.Empty<T>();
            }
        }
    }
}

namespace System.Collections.Immutable
{
    public static class ImmutableStackExtensions
    {
        /// <summary>
        /// Same as <see cref="Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/> but without any memory allocations when enumerating the stacks.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static bool SequenceEqual<TSource>(this ImmutableStack<TSource> first, ImmutableStack<TSource> second, IEqualityComparer<TSource>? comparer = null)
        {
            if (first == null)
                throw new ArgumentNullException(nameof(first));
            if (second == null)
                throw new ArgumentNullException(nameof(second));
            if (comparer == null)
                comparer = EqualityComparer<TSource>.Default;

            var e1 = first.GetEnumerator();
            var e2 = second.GetEnumerator();
            while (e1.MoveNext())
            {
                if (!(e2.MoveNext() && comparer.Equals(e1.Current, e2.Current)))
                {
                    return false;
                }
            }
            return !e2.MoveNext();
        }
    }

    public static class ImmutableArrayExtensions
    {
        static class DelegateCache<T>
        {
            static Func<ImmutableArray<T>.Builder, T[]>? getInternalArray;

            public static T[] GetInternalArray(ImmutableArray<T>.Builder builder)
            {
                const string elementsFieldName = "_elements";
                if (getInternalArray == null)
                {
                    var type = typeof(ImmutableArray<T>.Builder);
                    var _elementsField = type.GetField(elementsFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                    if (_elementsField == null)
                        throw new NotSupportedException($"Couldn't find field {elementsFieldName} in {type}");
                    var builderParameter = Expression.Parameter(type);
                    getInternalArray = Expression.Lambda<Func<ImmutableArray<T>.Builder, T[]>>(
                        body: Expression.Field(builderParameter, _elementsField),
                        parameters: builderParameter).Compile();
                }
                return getInternalArray(builder);
            }
        }

        public static T[] GetInternalArray<T>(this ImmutableArray<T>.Builder builder)
        {
            return DelegateCache<T>.GetInternalArray(builder);
        }

        public static Span<T> AsSpan<T>(this ImmutableArray<T>.Builder builder)
        {
            return builder.GetInternalArray().AsSpan(0, builder.Count);
        }
    }
}

namespace System
{
    public static class SpanExtensions
    {
        public static bool Any<T>(this ReadOnlySpan<T> source)
        {
            return source.Length > 0;
        }

        public static bool Any<T>(this ReadOnlySpan<T> source, Func<T, bool> predicate)
        {
            foreach (var x in source)
                if (predicate(x))
                    return true;
            return false;
        }

        public static bool All<T>(this ReadOnlySpan<T> source, Func<T, bool> predicate)
        {
            foreach (var x in source)
                if (!predicate(x))
                    return false;
            return true;
        }

        public static T ElementAt<T>(this ReadOnlySpan<T> source, int index)
        {
            return source[index];
        }

        public static T? ElementAtOrDefault<T>(this ReadOnlySpan<T> source, int index)
        {
            if (index >= 0 && index < source.Length)
                return source[index];
            return default;
        }
    }

    public static class DisposableExtensions
    {
        public static T DisposeBy<T>(this T component, ICollection<IDisposable> container) where T : IDisposable
        {
            container.Add(component);
            return component;
        }

        public static T DisposeBy<T>(this T component, AppHost appHost) where T : IDisposable
        {
            appHost.TakeOwnership(component);
            return component;
        }
    }
}
#nullable restore
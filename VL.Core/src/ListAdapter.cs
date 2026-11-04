#nullable enable
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace VL.Core;

/// <summary>
/// Lets everything that indexes into lists by path see lists the same way, also those that only implement <see cref="IList{T}"/> or <see cref="IReadOnlyList{T}"/>
/// and not the non-generic <see cref="IList"/>. Those are offered as an <see cref="IList"/> that forwards to the generic interface.
/// </summary>
static class ListAdapter
{
    static readonly ConcurrentDictionary<Type, Func<object, IList>?> factories = new();

    /// <summary>
    /// Gets the list of an instance. The instance itself if it is an <see cref="IList"/>, otherwise an adapter for its generic list interface.
    /// </summary>
    public static bool TryGetList(object? instance, [NotNullWhen(true)] out IList? list)
    {
        list = instance as IList;
        if (list is not null)
            return true;
        if (instance is null)
            return false;

        list = factories.GetOrAdd(instance.GetType(), CreateFactory)?.Invoke(instance);
        return list is not null;
    }

    // One adapter per generic interface, the one that can write is preferred
    static Func<object, IList>? CreateFactory(Type type)
    {
        var interfaces = type.GetInterfaces();
        foreach (var (generic, create) in new[] { (typeof(IList<>), nameof(CreateListAdapter)), (typeof(IReadOnlyList<>), nameof(CreateReadOnlyListAdapter)) })
        {
            var itf = interfaces.FirstOrDefault(i => i.IsConstructedGenericType && i.GetGenericTypeDefinition() == generic);
            if (itf is null)
                continue;

            var method = typeof(ListAdapter).GetMethod(create, BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(itf.GenericTypeArguments[0]);
            return (Func<object, IList>)method.CreateDelegate(typeof(Func<object, IList>));
        }
        return null;
    }

    static IList CreateListAdapter<T>(object instance) => new WritableListAdapter<T>((IList<T>)instance);

    static IList CreateReadOnlyListAdapter<T>(object instance) => new ReadOnlyListAdapter<T>((IReadOnlyList<T>)instance);

    /// <summary>
    /// What the adapters have in common: reading.
    /// </summary>
    abstract class ListAdapterBase<T> : IList
    {
        public abstract int Count { get; }
        public abstract object? this[int index] { get; set; }
        public abstract bool IsReadOnly { get; }
        public abstract bool IsFixedSize { get; }
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        protected abstract T GetItem(int index);

        public IEnumerator GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
                yield return GetItem(i);
        }

        public void CopyTo(Array array, int index)
        {
            foreach (var item in this)
                array.SetValue(item, index++);
        }

        public bool Contains(object? value) => IndexOf(value) >= 0;

        public int IndexOf(object? value)
        {
            for (var i = 0; i < Count; i++)
            {
                if (Equals(GetItem(i), value))
                    return i;
            }
            return -1;
        }

        public abstract int Add(object? value);
        public abstract void Clear();
        public abstract void Insert(int index, object? value);
        public abstract void Remove(object? value);
        public abstract void RemoveAt(int index);
    }

    /// <summary>
    /// For a list that only implements <see cref="IList{T}"/>: forwards everything.
    /// </summary>
    sealed class WritableListAdapter<T>(IList<T> list) : ListAdapterBase<T>
    {
        public override int Count => list.Count;
        public override bool IsReadOnly => list.IsReadOnly;
        public override bool IsFixedSize => list.IsReadOnly;
        protected override T GetItem(int index) => list[index];

        public override object? this[int index]
        {
            get => list[index];
            set => list[index] = (T)value!;
        }

        public override int Add(object? value)
        {
            list.Add((T)value!);
            return list.Count - 1;
        }

        public override void Clear() => list.Clear();
        public override void Insert(int index, object? value) => list.Insert(index, (T)value!);
        public override void Remove(object? value) => list.Remove((T)value!);
        public override void RemoveAt(int index) => list.RemoveAt(index);
    }

    /// <summary>
    /// For a list that only implements <see cref="IReadOnlyList{T}"/>: it can't change.
    /// </summary>
    sealed class ReadOnlyListAdapter<T>(IReadOnlyList<T> list) : ListAdapterBase<T>
    {
        public override int Count => list.Count;
        public override bool IsReadOnly => true;
        public override bool IsFixedSize => true;
        protected override T GetItem(int index) => list[index];

        public override object? this[int index]
        {
            get => list[index];
            set => throw new NotSupportedException();
        }

        public override int Add(object? value) => throw new NotSupportedException();
        public override void Clear() => throw new NotSupportedException();
        public override void Insert(int index, object? value) => throw new NotSupportedException();
        public override void Remove(object? value) => throw new NotSupportedException();
        public override void RemoveAt(int index) => throw new NotSupportedException();
    }
}
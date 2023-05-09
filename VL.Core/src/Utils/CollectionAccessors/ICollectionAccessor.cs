using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using VL.Lib.Reactive;

namespace VL.Core.Utils
{
    public interface ICollectionAccessor : IList
    {
        object UnderlyingCollection { get; set; }

        event EventHandler CollectionChanged;

        public static ICollectionAccessor Create(object collection)
        {
            var type = collection.GetType();
            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(List<>))
                    return CreateAccessor(typeof(ListAccessor<>).MakeGenericType(type.GetGenericArguments()[0]), collection);
                if (genericTypeDefinition == typeof(ImmutableList<>))
                    return CreateAccessor(typeof(ImmutableListAccessor<>).MakeGenericType(type.GetGenericArguments()[0]), collection);
            }
            return null;

            static ICollectionAccessor CreateAccessor(Type type, object collection)
            {
                var accessor = (ICollectionAccessor)Activator.CreateInstance(type);
                accessor.UnderlyingCollection = collection;
                return accessor;
            }
        }

        public static bool IsSupported(Type type)
        {
            if (type.IsArray)
                return false;

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(List<>))
                    return true;
                if (genericTypeDefinition == typeof(ImmutableList<>))
                    return true;
            }

            return false;
        }
    }
}

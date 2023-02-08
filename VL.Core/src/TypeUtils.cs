using System;

namespace VL.Core
{
    public static class TypeUtils
    {
        /// <summary>
        /// Creates a new instance of the specified type.
        /// </summary>
        public static object New(this Type type, NodeContext nodeContext = default)
        {
            var typeRegistry = TypeRegistry.Default;
            return typeRegistry.CreateInstance(type, nodeContext);
        }

        /// <summary>
        /// Creates a new instance of the specified type.
        /// </summary>
        public static T New<T>(NodeContext nodeContext = default) => New(typeof(T), nodeContext) is T v ? v : Default<T>();

        /// <summary>
        /// Returns the default value as defined by VL.
        /// </summary>
        public static object Default(this Type type)
        {
            var typeRegistry = TypeRegistry.Default;
            return typeRegistry.GetDefaultValue(type);
        }

        /// <summary>
        /// Returns the default value as defined by VL.
        /// </summary>
        public static T Default<T>() => Default(typeof(T)) is T v ? v : default(T);
    }
}

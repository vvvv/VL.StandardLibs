using System;
using System.Collections.Generic;

namespace VL.Core
{
    public abstract class SerializationService
    {
        /// <summary>
        /// Registers a VL serializer to the factory. 
        /// In case the type for which the serializer gets registered is generic a dummy type instantiation paired with a dummy
        /// serializer instantiation containing a public default constructor has to be registered. For example a serializer implementation
        /// of type FooSerializer&lt;T&gt; for the generic type Foo&lt;T&gt; has to be registered for the dummy instantiaton Foo&lt;object&gt;.
        /// </summary>
        /// <typeparam name="TForType">The type for which to register a serializer.</typeparam>
        /// <typeparam name="TSerializer">The type of the serializer implementation.</typeparam>
        /// <param name="factory">The factory in which the serializer gets registered.</param>
        /// <param name="serializer">The serializer to register.</param>
        /// <returns>The factory with the registered serializer.</returns>
        public abstract IVLFactory RegisterSerializer<TForType, TSerializer>(IVLFactory factory, TSerializer serializer = default(TSerializer)) 
            where TSerializer : class, ISerializer<TForType>;

        /// <summary>
        /// Whether or not an instance of the given type can be serialized.
        /// </summary>
        /// <param name="factory">The factory containing the serializer registrations.</param>
        /// <param name="forType">The type of the instance to serialize.</param>
        /// <returns>True if an instance of the given type can be serialized.</returns>
        public abstract bool CanSerialize(IVLFactory factory, Type forType);

        public object Serialize(NodeContext nodeContext, object value, Type staticType, bool includeDefaults, bool forceElement, bool pathsAreRelativeToDocument)
        {
            return Serialize(nodeContext, value, staticType, throwOnError: true, includeDefaults, forceElement, pathsAreRelativeToDocument, out _);
        }

        public object Serialize(NodeContext nodeContext, object value, Type staticType, bool includeDefaults, bool forceElement, bool pathsAreRelativeToDocument, out IReadOnlyList<string> errorMessages)
        {
            return Serialize(nodeContext, value, staticType, throwOnError: false, includeDefaults, forceElement, pathsAreRelativeToDocument, out errorMessages);
        }

        protected abstract object Serialize(NodeContext nodeContext, object value, Type staticType, bool throwOnError, bool includeDefaults, bool forceElement, bool pathsAreRelativeToDocument, out IReadOnlyList<string> errorMessages);

        public object Deserialize(NodeContext nodeContext, object content, Type type, bool pathsAreRelativeToDocument)
        {
            return Deserialize(nodeContext, content, type, throwOnError: true, pathsAreRelativeToDocument, out _);
        }

        public object Deserialize(NodeContext nodeContext, object content, Type type, bool pathsAreRelativeToDocument, out IReadOnlyList<string> errorMessages)
        {
            return Deserialize(nodeContext, content, type, throwOnError: false, pathsAreRelativeToDocument, out errorMessages);
        }

        protected abstract object Deserialize(NodeContext nodeContext, object content, Type type, bool throwOnError, bool pathsAreRelativeToDocument, out IReadOnlyList<string> errorMessages);
    }
}

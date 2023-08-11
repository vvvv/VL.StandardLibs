using System;
using System.Xml.Linq;

namespace VL.Core
{
    /// <summary>
    /// The serializer interface.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    public interface ISerializer<T>
    {
        /// <summary>
        /// Serializes the given value to a string, object[] or XElement.
        /// </summary>
        /// <param name="context">The context to use for serialization.</param>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The <see cref="string"/>, <see cref="T:Object[]"/> or <see cref="XElement"/>.</returns>
        object Serialize(SerializationContext context, T value);

        /// <summary>
        /// Deserializes the given content.
        /// </summary>
        /// <param name="context">The context to use for deserialization.</param>
        /// <param name="content">The content (string, object[] or XElement) to deserialize.</param>
        /// <param name="type">The type of the deserialized value.</param>
        /// <returns>The deserialized value.</returns>
        T Deserialize(SerializationContext context, object content, Type type);
    }

    /// <summary>
    /// The serialization context.
    /// </summary>
    public abstract class SerializationContext
    {
        /// <summary>
        /// The current app host.
        /// </summary>
        public abstract AppHost AppHost { get; }

        /// <summary>
        /// Serializes the given value and if a name is provided wraps the serialized content into an <see cref="XElement"/> or <see cref="XAttribute"/>. 
        /// </summary>
        /// <typeparam name="T">
        /// The statically known type of the value. In case it differes from the runtime type of the value 
        /// the serialized content will always be wrapped in an <see cref="XElement"/> with additional type information.
        /// </typeparam>
        /// <param name="name">The name to use (if any) for the element or attribute wrapping the serialized content.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="forceElement">If true the content will always be wrapped in an <see cref="XElement"/>.</param>
        /// <returns>The serialized content or if any wrapping happended the <see cref="XAttribute"/> or <see cref="XElement"/>.</returns>
        public object Serialize<T>(string name, T value, bool forceElement = false)
        {
            return Serialize(name, value, typeof(T), forceElement); // Box
        }

        /// <summary>
        /// Serializes the given value and if a name is provided wraps the serialized content into an <see cref="XElement"/> or <see cref="XAttribute"/>. 
        /// </summary>
        /// <param name="name">The name to use (if any) for the element or attribute wrapping the serialized content.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="staticType">
        /// The statically known type of the value. In case it differes from the runtime type of the value 
        /// the serialized content will always be wrapped in an <see cref="XElement"/> with additional type information.
        /// </param>
        /// <param name="forceElement">If true the content will always be wrapped in an <see cref="XElement"/>.</param>
        /// <returns>The serialized content or if any wrapping happended the <see cref="XAttribute"/> or <see cref="XElement"/>.</returns>
        public abstract object Serialize(string name, object value, Type staticType, bool forceElement = false);

        /// <summary>
        /// Deserializes the given content or if a name is provided extracts and deserializes the attribute or child element with the given name.
        /// </summary>
        /// <typeparam name="T">The statically known type of the value to deserialize.</typeparam>
        /// <param name="content">The content to deserialize.</param>
        /// <param name="name">The name of the attribute or child element to extract from the content and deserialize.</param>
        /// <returns>The deserialized value or the given default value in case deserialization failed.</returns>
        public T Deserialize<T>(object content, string name)
        {
            return (T)Deserialize(content, name, typeof(T)); // Unbox
        }

        /// <summary>
        /// Deserializes the given content or if a name is provided extracts and deserializes the attribute or child element with the given name.
        /// </summary>
        /// <param name="content">The content to deserialize.</param>
        /// <param name="name">The name of the attribute or child element to extract from the content and deserialize.</param>
        /// <param name="staticType">The statically known type of the value to deserialize.</param>
        /// <returns>The deserialized value or the default value in case deserialization failed.</returns>
        public abstract object Deserialize(object content, string name, Type staticType);
    }

    /// <summary>
    /// Represents errors that occur during serialization.
    /// </summary>
    public class SerializationException : Exception
    {
        /// <summary>
        /// Creates a new instances of the <see cref="SerializationException"/> class with the specified error message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public SerializationException(string message) : base(message)
        {

        }
    }
    
    public static partial class Serialization
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
        public static IVLFactory RegisterSerializer<TForType, TSerializer>(this IVLFactory factory, TSerializer serializer = default(TSerializer))
            where TSerializer : class, ISerializer<TForType>
        {
            return factory.AppHost.Services.GetService<SerializationService>().RegisterSerializer<TForType, TSerializer>(factory, serializer);
        }

        /// <summary>
        /// Whether or not an instance of the given type can be serialized.
        /// </summary>
        /// <param name="factory">The factory containing the serializer registrations.</param>
        /// <param name="forType">The type of the instance to serialize.</param>
        /// <returns>True if an instance of the given type can be serialized.</returns>
        public static bool CanSerialize(this IVLFactory factory, Type forType)
        {
            return factory.AppHost.Services.GetService<SerializationService>().CanSerialize(factory, forType);
        }
    }
}

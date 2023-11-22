using System;
using System.Collections.Generic;
using System.Xml.Linq;
using VL.Lib.Collections;

namespace VL.Core
{
    /// <summary>
    /// Serialization related extension methods and constants.
    /// </summary>
    public static partial class Serialization
    {
        /// <summary>
        /// Serializes the given value into an <see cref="XElement"/>.
        /// </summary>
        /// <remarks>
        /// Paths are made relative to the application entry point.
        /// Throws a <see cref="SerializationException"/> in case serialization fails.
        /// </remarks>
        /// <typeparam name="T">
        /// The statically known type of the value. 
        /// In case it differs from the runtime type a type annotation will be added to the serialized content.
        /// </typeparam>
        /// <param name="nodeContext">The node context to use for serialization.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="includeDefaults">Whether or not default values should be serialized.</param>
        /// <returns>The serialized content as an <see cref="XElement"/>.</returns>
        public static XElement Serialize<T>(this NodeContext nodeContext, T value, bool includeDefaults = false)
        {
            var serialization = nodeContext.AppHost.SerializationService;
            return (XElement)serialization.Serialize(nodeContext, value, typeof(T), includeDefaults, pathsAreRelativeToDocument: false, forceElement: true);
        }

        /// <summary>
        /// Serializes the given value into an <see cref="XElement"/>. 
        /// </summary>
        /// <remarks>
        /// Paths are made relative to the application entry point.
        /// </remarks>
        /// <typeparam name="T">
        /// The statically known type of the value. 
        /// In case it differs from the runtime type a type annotation will be added to the serialized content.
        /// </typeparam>
        /// <param name="nodeContext">The node context to use for serialization.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="throwOnError">Whether or not serialization errors should lead to an exception.</param>
        /// <param name="includeDefaults">If true default values will also be serialized.</param>
        /// <param name="errorMessages">The accumulated error messages in case <paramref name="throwOnError"/> is disabled.</param>
        /// <returns>The serialized content as an <see cref="XElement"/>.</returns>
        public static XElement Serialize<T>(this NodeContext nodeContext, T value, bool throwOnError, bool includeDefaults, out IReadOnlyList<string> errorMessages)
        {
            var serialization = nodeContext.AppHost.SerializationService;
            if (throwOnError)
            {
                errorMessages = Spread<string>.Empty;
                return (XElement)serialization.Serialize(nodeContext, value, typeof(T), includeDefaults, forceElement: true, pathsAreRelativeToDocument: false);
            }
            else
            {
                return (XElement)serialization.Serialize(nodeContext, value, typeof(T), includeDefaults, forceElement: true, pathsAreRelativeToDocument: false, out errorMessages);
            }
        }

        /// <summary>
        /// Deserializes the given content.
        /// </summary>
        /// <remarks>
        /// Relative paths are made absolute to the application entry point.
        /// Throws a <see cref="SerializationException"/> in case deserialization fails.
        /// </remarks>
        /// <typeparam name="T">The statically known type of the value to deserialize.</typeparam>
        /// <param name="nodeContext">The node context to use for deserialization.</param>
        /// <param name="content">The content to deserialize.</param>
        /// <returns>The deserialized value.</returns>
        public static T Deserialize<T>(this NodeContext nodeContext, XElement content)
        {
            var serialization = nodeContext.AppHost.SerializationService;
            return (T)serialization.Deserialize(nodeContext, content, typeof(T), pathsAreRelativeToDocument: false);
        }

        /// <summary>
        /// Deserializes the given content.
        /// </summary>
        /// <remarks>
        /// Relative paths are made absolute to the application entry point.
        /// </remarks>
        /// <typeparam name="T">The statically known type of the value to deserialize.</typeparam>
        /// <param name="nodeContext">The node context to use for deserialization.</param>
        /// <param name="content">The content to deserialize.</param>
        /// <param name="throwOnError">Whether or not deserialization errors should lead to an exception.</param>
        /// <param name="errorMessages">The accumulated error messages in case <paramref name="throwOnError"/> is disabled.</param>
        /// <returns>The deserialized value.</returns>
        public static T Deserialize<T>(this NodeContext nodeContext, XElement content, bool throwOnError, out IReadOnlyList<string> errorMessages)
        {
            var serialization = nodeContext.AppHost.SerializationService;
            if (throwOnError)
            {
                errorMessages = Spread<string>.Empty;
                return (T)serialization.Deserialize(nodeContext, content, typeof(T), pathsAreRelativeToDocument: false);
            }
            else
            {
                return (T)serialization.Deserialize(nodeContext, content, typeof(T), pathsAreRelativeToDocument: false, out errorMessages);
            }
        }
    }
}

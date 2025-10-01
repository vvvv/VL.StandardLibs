using CommunityToolkit.HighPerformance;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
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
            return Serialize(nodeContext, value, typeof(T), includeDefaults);
        }

        // Non-generic variant for unit tests
        internal static XElement Serialize(NodeContext nodeContext, object value, Type staticType, bool includeDefaults = false)
        {
            var serialization = nodeContext.AppHost.SerializationService;
            return (XElement)serialization.Serialize(nodeContext, value, staticType, includeDefaults, pathsAreRelativeToDocument: false, forceElement: true);
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
            return (T)Deserialize(nodeContext, content, typeof(T));
        }

        // Non-generic variant for unit tests
        internal static object Deserialize(NodeContext nodeContext, XElement content, Type staticType)
        {
            var serialization = nodeContext.AppHost.SerializationService;
            return serialization.Deserialize(nodeContext, content, staticType, pathsAreRelativeToDocument: false);
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

#nullable enable

        private static char BOM = char.ConvertFromUtf32(0xFEFF)[0];

        private static bool StartsWithBOM(string content)
        {
            return content.Length > 1 && content[0] == BOM;
        }

        internal static string? Encode(string? content)
        {
            if (content is null)
                return null;

            if (StartsWithBOM(content))
                goto encode;

            try
            {
                return XmlConvert.VerifyXmlChars(content);
            }
            catch
            {
                goto encode;
            }

            encode:
            var bytes = content.AsSpan().AsBytes();
            return BOM + Convert.ToBase64String(bytes);
        }

        internal static string? Decode(string? content)
        {
            if (content is null)
                return null;

            if (StartsWithBOM(content))
            {
                // Remove the flag
                var encodedString = content.AsSpan(1);
                var bytes = ArrayPool<byte>.Shared.Rent(encodedString.AsBytes().Length);
                Convert.TryFromBase64Chars(encodedString, bytes, out var bytesWritten);
                return new string(bytes.AsSpan(0, bytesWritten).Cast<byte, char>());
            }
            else
            {
                return content;
            }
        }
#nullable restore
    }
}

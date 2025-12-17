using System;
using System.Xml.Linq;

namespace VL.Core.EditorAttributes
{
    /// <summary>
    /// Default value for a property. Can be in short form or properly deserialized as known from other places in VL.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DefaultAttribute : Attribute
    {
        /// <summary>
        /// Computes an encoded value suitable for use with <see cref="DefaultAttribute"/>.
        /// Attempts to encode the value in a simple string format for primitive types (int, float, vectors, etc.),
        /// or falls back to full XML serialization for complex types.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <param name="nodeContext">The node context used for serialization. If null, a default context is created using the current or global AppHost.</param>
        /// <param name="typeHint">Feeding a typeHint may abbreviate the serialization. Only feed a typeHint if you know that the type is known on deserialization.</param>
        /// <returns>A string representation of the value, either in simple format or as an XML string.</returns>
        public static string ComputeEncodedValue(object value, NodeContext nodeContext = default, Type typeHint = default)
        {
            nodeContext = nodeContext ?? NodeContext.Create(AppHost.CurrentOrGlobal);
            var optional = AttributeHelpers.EncodeValueForAttribute(value);
            if (optional.HasValue)
                return optional.Value;
            else
                return Serialization.Serialize(nodeContext, value, typeHint ?? typeof(object)).ToString();
        }

        /// <summary>
        /// The default value of the property. Does not guearantee that the property gets initialized like that. Only a hint for editors.
        /// </summary>
        public DefaultAttribute(string encodedValue)
        {
            EncodedValue = encodedValue;
        }

        public string EncodedValue { get; }

        public T GetValue<T>(NodeContext nodeContext = default) => (T)GetValue(typeof(T), nodeContext);

        public object GetValue(Type type, NodeContext nodeContext = default)
        {
            nodeContext = nodeContext ?? NodeContext.Create(AppHost.CurrentOrGlobal);
            if (EncodedValue.StartsWith('<'))
                return Serialization.Deserialize(nodeContext, XElement.Parse(EncodedValue), type);

            var optional = AttributeHelpers.DecodeValueFromAttribute(EncodedValue, type);
            if (optional.HasValue)
                return optional.Value;
            else
                return Serialization.Deserialize(nodeContext, XElement.Parse(EncodedValue), type);
        }

        public override string ToString()
        {
            return $"Default: {EncodedValue}";
        }
    }
}

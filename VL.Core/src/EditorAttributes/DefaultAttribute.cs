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
        NodeContext NodeContext;

        /// <summary>
        /// The default value of the property. Does not guearantee that the property gets initialized like that. Only a hint for editors.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="nodeContext"></param>
        /// <param name="typeHint">Feeding a typeHint may abbreviate the serialization. Only feed a typeHint if you know that the type is known on deserialization.</param>
        public DefaultAttribute(object value, NodeContext nodeContext, Type typeHint = default)
        {
            NodeContext = nodeContext ?? NodeContext.Create(AppHost.CurrentOrGlobal);
            var optional = AttributeHelpers.EncodeValueForAttribute(value);
            if (optional.HasValue)
                EncodedValue = optional.Value;
            else
                EncodedValue = Serialization.Serialize(NodeContext, value, typeHint ?? typeof(object)).ToString();
        }

        public string EncodedValue { get; }

        public T GetValue<T>()
        {
            if (EncodedValue.StartsWith('<'))
                return Serialization.Deserialize<T>(NodeContext, XElement.Parse(EncodedValue));

            var optional = AttributeHelpers.DecodeValueFromAttribute<T>(EncodedValue);
            if (optional.HasValue)
                return optional.Value;
            else
                return Serialization.Deserialize<T>(NodeContext, XElement.Parse(EncodedValue));
        }

        public override string ToString()
        {
            return $"Default: {EncodedValue}";
        }
    }
}

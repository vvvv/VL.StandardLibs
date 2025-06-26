using System;

namespace VL.Core.EditorAttributes
{

    /// <summary>
    /// Current supported types: number types, Vector2, Vector3, RGBA, Int2, Int3
    /// You may feed a single value for vectors and colors, which will then be used for all dimensions
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class MaxAttribute : Attribute
    {
        public MaxAttribute(string encodedValue)
        {
            EncodedValue = encodedValue;
        }

        public MaxAttribute(object value)
        {
            EncodedValue = AttributeHelpers.EncodeValueForAttribute(value).Value;
        }

        public string EncodedValue { get; }

        public T GetValue<T>() => (T)GetValue(typeof(T));

        public object GetValue(Type type)
        {
            var optional = AttributeHelpers.DecodeValueFromAttribute(EncodedValue, type);
            if (optional.HasValue)
                return optional.Value;
            return default;
        }

        public override string ToString()
        {
            return $"Max: {EncodedValue}";
        }
    }
}

using System;

namespace VL.Core.EditorAttributes
{

    /// <summary>
    /// Current supported types: int, float, double, Vector2, Vector3, RGBA 
    /// You may feed a single value for vectors and colors, which will then be used for all dimensions
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class MinAttribute : Attribute
    {
        public MinAttribute(string encodedValue)
        {
            EncodedValue = encodedValue;
        }

        public MinAttribute(float value)
        {
            EncodedValue = AttributeHelpers.EncodeValueForAttribute(value).Value;
        }

        public string EncodedValue { get; }

        public T GetValue<T>() => AttributeHelpers.DecodeValueFromAttribute<T>(EncodedValue).Value;

        public override string ToString()
        {
            return $"Min: {EncodedValue}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Core.EditorAttributes
{

    /// <summary>
    /// Current supported types: int, float, double, Vector2, Vector3, RGBA 
    /// You may feed a single value for vectors and colors, which will then be used for all dimensions
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class MaxAttribute : Attribute
    {
        public MaxAttribute(object value)
        {
            EncodedValue = AttributeHelpers.EncodeValueForAttribute(value);
        }

        public string EncodedValue { get; }

        public T GetValue<T>() => AttributeHelpers.DecodeValueFromAttribute<T>(EncodedValue);

        public override string ToString()
        {
            return $"Max: {EncodedValue}";
        }
    }
}

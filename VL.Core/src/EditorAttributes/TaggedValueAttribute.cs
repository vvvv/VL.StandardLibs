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
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class TaggedValueAttribute : Attribute
    {
        public TaggedValueAttribute(string key, object value)
        {
            Key = key;
            EncodedValue = AttributeHelpers.EncodeValueForAttribute(value).Value;
        }

        public string Key { get; }

        string EncodedValue { get; }

        public T GetValue<T>() => AttributeHelpers.DecodeValueFromAttribute<T>(EncodedValue).Value;

        //public static string DefaultKey = "Default";

        public override string ToString()
        {
            return $"{Key}: {EncodedValue}";
        }
    }
}

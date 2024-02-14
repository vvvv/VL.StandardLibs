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
    internal class TaggedValueAttribute : Attribute
    {
        public TaggedValueAttribute(string key, object value)
        {
            Key = key;
            EncodeValue = AttributeHelpers.EncodeValueForAttribute(value);
        }

        public string Key { get; }

        string EncodeValue { get; }

        public T GetValue<T>() => AttributeHelpers.DecodeValueFromAttribute<T>(EncodeValue);

        public static string MinKey = "Min";
        public static string MaxKey = "Max";
        public static string DefaultKey = "Default";
    }
}

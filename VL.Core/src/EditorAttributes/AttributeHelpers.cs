using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VL.Core.EditorAttributes
{
    public static class AttributeHelpers
    {
        /// <summary>
        /// Restrictions of the .Net attribute system force us to work with encoded values.
        /// Attributes in the VL.Core.EditorAttributes namespace work with string properties to store numeric values.
        /// The methods EncodeValueForAttribute and DecodeValueFromAttribute define how values   
        /// of types common in the VL eco system are stored in an attribute. 
        /// We want to protect the user from the need to guess how to deserialize. 
        /// Furthermore we expect that you know what type the value should have on deserialize.
        /// For now only a handful of types are supported: int, float, double, Vector2, Vector3, RGBA
        /// more primitive types might get added later on.
        /// JSON serialization might be added for other types later on. (The first character being a {)
        /// </summary>
        public static string EncodeValueForAttribute(object value)
        {
            if (value is null)
                return null;

            if (value is int || 
                value is float ||
                value is double)
                return value.ToString();

            if (value is Vector2 v2)
                return $"{v2.X}, {v2.Y}";

            if (value is Vector3 v3)
                return $"{v3.X}, {v3.Y}, {v3.Z}";

            if (value is Color4 c4)
                return $"{c4.R}, {c4.G}, {c4.B}, {c4.A}";

            throw new NotImplementedException();
        }

        public static object DecodeValueFromAttribute(string encodedValue, Type targetType)
        {
            if (targetType == typeof(int))
                return int.Parse(encodedValue);

            if (targetType == typeof(float))
                return float.Parse(encodedValue);
            
            if (targetType == typeof(double))
                return double.Parse(encodedValue);
            
            var values = encodedValue.Split(',')
                .Select(DecodeValueFromAttribute<float>)
                .ToArray();

            var firstSlice = values.Length > 0 ? values[0] : default;

            var resampledValues =
                new[] { 0, 1, 2, 3 }
                .Select(i => values.Length > i ? values[i] : firstSlice)
                .ToArray();

            if (targetType == typeof(Vector2))
                return new Vector2(resampledValues[0], resampledValues[1]);

            if (targetType == typeof(Vector3))
                return new Vector3(resampledValues[0], resampledValues[1], resampledValues[2]);

            if (targetType == typeof(Color4))
                return new Color4(resampledValues[0], resampledValues[1], resampledValues[2], resampledValues[3]);

            throw new NotImplementedException();
        }

        public static T DecodeValueFromAttribute<T>(string encodedValue)
            => (T)DecodeValueFromAttribute(encodedValue, typeof(T));
    }
}

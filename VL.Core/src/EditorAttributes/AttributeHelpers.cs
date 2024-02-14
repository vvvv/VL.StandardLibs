using Stride.Core.Mathematics;
using System;
using System.Collections.Immutable;
using System.Linq;
using VL.Lib.Collections;

namespace VL.Core.EditorAttributes
{
    public static class AttributeHelpers
    {
        public static Optional<string> GetLabel(this IHasAttributes propertyInfoOrChannel)
        {
            var labelAttribute = propertyInfoOrChannel.GetAttributes<LabelAttribute>().FirstOrDefault();
            if (labelAttribute != null)
                return labelAttribute.Label;

            var displayAttribute = propertyInfoOrChannel.GetAttributes<Stride.Core.DisplayAttribute>().FirstOrDefault();
            if (displayAttribute != null)
                return displayAttribute.Name;

            var displayAttribute2 = propertyInfoOrChannel.GetAttributes<System.ComponentModel.DataAnnotations.DisplayAttribute>().FirstOrDefault();
            if (displayAttribute2 != null)
                return displayAttribute2.Name;

            return new Optional<string>();
        }

        public static Optional<string> GetDescription(this IHasAttributes propertyInfoOrChannel)
        {
            var displayAttribute = propertyInfoOrChannel.GetAttributes<System.ComponentModel.DataAnnotations.DisplayAttribute>().FirstOrDefault();
            if (displayAttribute != null)
                return displayAttribute.Description;

            return new Optional<string>();
        }

        public static Optional<int> GetOrder(this IHasAttributes propertyInfoOrChannel)
        {
            var displayAttribute = propertyInfoOrChannel.GetAttributes<System.ComponentModel.DataAnnotations.DisplayAttribute>().FirstOrDefault();
            if (displayAttribute != null)
            {
                var o = displayAttribute.GetOrder();
                if (o != null)
                    return new Optional<int>(o.Value);
            }
            return new Optional<int>();
        }

        public static Optional<WidgetType> GetWidgetType(this IHasAttributes propertyInfoOrChannel)
        {
            var attr = propertyInfoOrChannel.GetAttributes<WidgetTypeAttribute>().FirstOrDefault();
            if (attr != null)
                return attr.WidgetType;
            return new Optional<WidgetType>();
        }

        public static bool GetIsExposed(this IHasAttributes propertyInfoOrChannel)
        {
            var attr = propertyInfoOrChannel.GetAttributes<ExposedAttribute>().FirstOrDefault();
            if (attr != null)
                return true;
            return false;
        }

        public static Spread<string> GetTags(this IHasAttributes propertyInfoOrChannel)
        {
            var attr = propertyInfoOrChannel.GetAttributes<TagAttribute>();
            return attr.Select(a => a.TagLabel).ToSpread();
        }

        public static ImmutableDictionary<string, string> GetCustomMetaData(this IHasAttributes propertyInfoOrChannel)
        {
            var attr = propertyInfoOrChannel.GetAttributes<CustomMetaDataAttribute>();
            return attr.Distinct(a => a.Key).ToImmutableDictionary(a =>  a.Key, a => a.Value);
        }


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

        public static bool HasTaggedValue(this IHasAttributes propertyInfoOrChannel, string key)
            => propertyInfoOrChannel.GetAttributes<TaggedValueAttribute>().Any(a => a.Key == key);

        public static Optional<T> GetTaggedValue<T>(this IHasAttributes propertyInfoOrChannel, string key)
        {
            var attr = propertyInfoOrChannel.GetAttributes<TaggedValueAttribute>().FirstOrDefault(a => a.Key == key);
            return attr != null ? new Optional<T>(attr.GetValue<T>()) : new Optional<T>();
        }

        public static Optional<T> GetMin<T>(this IHasAttributes propertyInfoOrChannel) 
            => GetTaggedValue<T>(propertyInfoOrChannel, TaggedValueAttribute.MinKey);

        public static Optional<T> GetMax<T>(this IHasAttributes propertyInfoOrChannel)
            => GetTaggedValue<T>(propertyInfoOrChannel, TaggedValueAttribute.MaxKey);

        public static Optional<T> GetDefault<T>(this IHasAttributes propertyInfoOrChannel) 
            => GetTaggedValue<T>(propertyInfoOrChannel, TaggedValueAttribute.DefaultKey);

        public static Optional<T> GetStepSize<T>(this IHasAttributes propertyInfoOrChannel) 
            => GetTaggedValue<T>(propertyInfoOrChannel, TaggedValueAttribute.StepSizeKey);
    }
}

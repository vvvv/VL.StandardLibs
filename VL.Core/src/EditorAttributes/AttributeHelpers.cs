﻿using Stride.Core.Mathematics;
using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using VL.Core.Utils;
using VL.Lib.Collections;
using VL.Lib.IO;

namespace VL.Core.EditorAttributes
{
    public static class AttributeHelpers
    {
        public static Optional<string> GetLabel(this IHasAttributes propertyInfoOrChannel)
        {
            foreach (var attr in propertyInfoOrChannel.Attributes)
            {
                if (attr is LabelAttribute l)
                    return l.Label;
                if (attr is Stride.Core.DisplayAttribute d)
                    return d.Name;
                if (attr is System.ComponentModel.DataAnnotations.DisplayAttribute d1)
                    return d1.Name;
            }

            return new Optional<string>();
        }

        public static Optional<string> GetDescription(this IHasAttributes propertyInfoOrChannel)
        {
            foreach (var attr in propertyInfoOrChannel.Attributes)
            {
                if (attr is DescriptionAttribute d)
                    return d.Description;
                if (attr is System.ComponentModel.DataAnnotations.DisplayAttribute d1)
                    return d1.Description;
            }

            return new Optional<string>();
        }

        public static Optional<int> GetOrder(this IHasAttributes propertyInfoOrChannel)
        {
            foreach (var attr in propertyInfoOrChannel.Attributes)
            {
                if (attr is OrderAttribute oa)
                    return oa.Order;

                if (attr is System.ComponentModel.DataAnnotations.DisplayAttribute d)
                {
                    var o = d.GetOrder();
                    if (o is not null)
                        return new Optional<int>(o.Value);
                }
            }

            return new Optional<int>();
        }

        public static Optional<WidgetType> GetWidgetType(this IHasAttributes propertyInfoOrChannel)
        {
            foreach (var attr in propertyInfoOrChannel.Attributes)
            {
                if (attr is WidgetTypeAttribute w)
                    return w.WidgetType;
            }

            return new Optional<WidgetType>();
        }

        public static bool GetCanBePublished(this IHasAttributes propertyInfoOrChannel, bool fallback = true)
        {
            foreach (var attr in propertyInfoOrChannel.Attributes)
            {
                if (attr is CanBePublishedAttribute a)
                    return a.CanBePublished;
            }

            return fallback;
        }

        public static bool GetIsBrowsable(this IHasAttributes propertyInfoOrChannel)
        {
            foreach (var attr in propertyInfoOrChannel.Attributes)
            {
                if (attr is BrowsableAttribute a)
                    return a.Browsable;
            }

            return true;
        }

        public static bool GetIsReadOnly(this IHasAttributes propertyInfoOrChannel)
        {
            foreach (var attr in propertyInfoOrChannel.Attributes)
            {
                if (attr is ReadOnlyAttribute r)
                    return true;
                if (attr is System.ComponentModel.ReadOnlyAttribute r2)
                    return r2.IsReadOnly;
            }

            return false;
        }


        public static Spread<string> GetTags(this IHasAttributes propertyInfoOrChannel) => propertyInfoOrChannel.Tags;

        public static ImmutableDictionary<string, string> GetCustomMetaData(this IHasAttributes propertyInfoOrChannel)
        {
            var builder = Pooled.GetDictionaryBuilder<string, string>();

            foreach (var attr in propertyInfoOrChannel.Attributes)
            {
                if (attr is CustomMetaDataAttribute c)
                    builder.Value[c.Key] = c.Value;
            }

            return builder.ToImmutableAndFree();
        }

        public static bool IsOfSupportedType(Type type)
        {
            return
                type == typeof(int) ||
                type == typeof(float) ||
                type == typeof(double) ||
                type == typeof(Vector2) ||
                type == typeof(Vector3) ||
                type == typeof(Color4);
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
        public static Optional<string> EncodeValueForAttribute(object value)
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

            return new Optional<string>();
        }

        static T[] parse<T>(string encodedValue, Func<string, T> parseCore)
        {
            var values = encodedValue.Split(',')
                .Select(x => parseCore(x))
                .ToArray();

            var firstSlice = values.Length > 0 ? values[0] : default;
            return
                new[] { 0, 1, 2, 3 }
                .Select(i => values.Length > i ? values[i] : firstSlice)
                .ToArray();
        }

        public static Optional<object> DecodeValueFromAttribute(string encodedValue, Type targetType)
        {
            try
            {
                if (targetType == typeof(int))
                    return parse(encodedValue, int.Parse)[0];
                if (targetType == typeof(double))
                    return parse(encodedValue, double.Parse)[0];
            }
            catch (Exception)
            {
                try
                {
                    var floats = parse(encodedValue, float.Parse);
                    if (targetType == typeof(int))
                        return (int)floats[0];
                    if (targetType == typeof(double))
                        return (double)floats[0];
                }
                catch (Exception)
                {
                    return new Optional<object>();
                }
            }

            try
            {
                var floats = parse(encodedValue, float.Parse);
                if (targetType == typeof(float))
                    return floats[0];
                if (targetType == typeof(Vector2))
                    return new Vector2(floats[0], floats[1]);
                if (targetType == typeof(Vector3))
                    return new Vector3(floats[0], floats[1], floats[2]);
                if (targetType == typeof(Color4))
                    return new Color4(floats[0], floats[1], floats[2], floats[3]);

                return new Optional<object>();
            }
            catch (Exception)
            {
                return new Optional<object>();
            }
        }

        public static Optional<T> DecodeValueFromAttribute<T>(string encodedValue)
            => DecodeValueFromAttribute(encodedValue, typeof(T))
                .Project(x => (T)x);

        public static bool HasTaggedValue(this IHasAttributes propertyInfoOrChannel, string key)
        {
            foreach (var a in propertyInfoOrChannel.Attributes)
                if (a is TaggedValueAttribute t && t.Key == key)
                    return true;
            return false;
        }

        public static Optional<T> GetTaggedValue<T>(this IHasAttributes propertyInfoOrChannel, string key)
        {
            foreach (var a in propertyInfoOrChannel.Attributes)
                if (a is TaggedValueAttribute t && t.Key == key)
                    return t.GetValue<T>();

            return default;
        }

        public static Optional<T> GetMin<T>(this IHasAttributes propertyInfoOrChannel)
        {
            foreach (var a in propertyInfoOrChannel.Attributes)
                if (a is MinAttribute m)
                    return m.GetValue<T>();

            return default;
        }

        public static Optional<T> GetMax<T>(this IHasAttributes propertyInfoOrChannel)
        {
            foreach (var a in propertyInfoOrChannel.Attributes)
                if (a is MaxAttribute m)
                    return m.GetValue<T>();

            return default;
        }

        public static Optional<T> GetDefault<T>(this IHasAttributes propertyInfoOrChannel)
        {
            foreach (var a in propertyInfoOrChannel.Attributes)
                if (a is DefaultAttribute m)
                    return m.GetValue<T>();

            return default;
        }

        //public static Optional<T> GetStepSize<T>(this IHasAttributes propertyInfoOrChannel) 
        //    => GetTaggedValue<T>(propertyInfoOrChannel, TaggedValueAttribute.StepSizeKey);


    }
}
